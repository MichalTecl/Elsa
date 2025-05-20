IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'AssignTagToCustomers')
	DROP PROCEDURE AssignTagToCustomers;

GO

CREATE PROCEDURE AssignTagToCustomers(@authorId INT, @tagTypeId INT, @note NVARCHAR(2000), @customerIds IntTable READONLY)
AS
BEGIN
	BEGIN TRAN;

	/* 1. skip already assigned */	
	DECLARE @toAssign TABLE(customerId INT);
	INSERT INTO @toAssign
	SELECT i.Id
	  FROM @customerIds i
	 WHERE NOT EXISTS(SELECT TOP 1 1 
                        FROM CustomerTagAssignment ex 
                       WHERE ex.TagTypeId = @tagTypeId 
                         AND ex.CustomerId = i.Id 
                         AND ex.UnassignDt IS NULL);
    
	/* 2. Delete assignments by transition */
    /* 2.1 - delete assignments w/o note */
	DELETE FROM CustomerTagAssignment 
	WHERE Note IS NULL
      AND Id IN (
		SELECT asig.Id
		  FROM @toAssign ta
		  JOIN CustomerTagAssignment asig ON (asig.CustomerId = ta.customerId)
		  JOIN CustomerTagTransition tr ON (tr.SourceTagTypeId = asig.TagTypeId)	  
		 WHERE tr.TargetTagTypeId = @tagTypeId
           AND asig.UnassignDt IS NULL
	 );

     /* 2.2 Set UnassignDt instead of hard delete where note is present*/
     UPDATE CustomerTagAssignment SET UnassignDt = GETDATE()     
	  WHERE Note IS NULL
      AND Id IN (
		SELECT asig.Id
		  FROM @toAssign ta
		  JOIN CustomerTagAssignment asig ON (asig.CustomerId = ta.customerId)
		  JOIN CustomerTagTransition tr ON (tr.SourceTagTypeId = asig.TagTypeId)	  
		 WHERE tr.TargetTagTypeId = @tagTypeId
           AND asig.UnassignDt IS NULL
	 );

	/* 3. create assignments */
	INSERT INTO CustomerTagAssignment (TagTypeId, AssignDt, CustomerId, AuthorId, Note)
	SELECT @tagTypeId, GETDATE(), ta.customerId, @authorId, @note
	 FROM @toAssign ta;
	
	SELECT * FROM @toAssign;

	COMMIT;	
END

