IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'AssignTagToCustomers')
	DROP PROCEDURE AssignTagToCustomers;

GO

CREATE PROCEDURE AssignTagToCustomers(@authorId INT, @tagTypeId INT,  @customerIds IntTable READONLY)
AS
BEGIN
	BEGIN TRAN;

	/* 1. skip already assigned */	
	DECLARE @toAssign TABLE(customerId INT);
	INSERT INTO @toAssign
	SELECT i.Id
	  FROM @customerIds i
	 WHERE NOT EXISTS(SELECT TOP 1 1 FROM CustomerTagAssignment ex WHERE ex.TagTypeId = @tagTypeId AND ex.CustomerId = i.Id);

    
	/* 2. Delete assignments by transition */
	DELETE FROM CustomerTagAssignment 
	WHERE Id IN (
		SELECT asig.Id
		  FROM @toAssign ta
		  JOIN CustomerTagAssignment asig ON (asig.CustomerId = ta.customerId)
		  JOIN CustomerTagTransition tr ON (tr.SourceTagTypeId = asig.TagTypeId)	  
		 WHERE tr.TargetTagTypeId = @tagTypeId
	 );

	/* 3. create assignments */
	INSERT INTO CustomerTagAssignment (TagTypeId, AssignDt, CustomerId, AuthorId)
	SELECT @tagTypeId, GETDATE(), ta.customerId, @authorId
	 FROM @toAssign ta;
	
	SELECT * FROM @toAssign;

	COMMIT;	
END

