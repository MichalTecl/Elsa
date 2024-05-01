IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_missingStoreAddress')
	DROP PROCEDURE insp_missingStoreAddress;

GO

CREATE PROCEDURE [dbo].[insp_missingStoreAddress] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	DECLARE @isu TABLE (customerId INT, customerName NVARCHAR(255));
	INSERT INTO @isu
	SELECT c.Id, c.Name
  FROM Customer c
  WHERE c.ProjectId = @projectId
    AND c.IsValuableDistributor = 1
    AND ISNULL(c.HasStore, 0) = 1
	AND NOT EXISTS(SELECT TOP 1 1 
	                 FROM CustomerStore cs 
					WHERE cs.CustomerId = c.Id);
		   	   		    	
     WHILE(EXISTS(SELECT TOP 1 1 FROM @isu))
	 BEGIN
		DECLARE @customerId INT,
				@customerName NVARCHAR(255);

		SELECT TOP 1
			@customerId = customerId,
			@customerName = customerName
		FROM
			@isu;

		DECLARE @code NVARCHAR(200);
		DECLARE @message NVARCHAR(2000);
	
		SET @code = 'missingStoreAddress_' + LTRIM(STR(@customerId));
		SET @message = N'Aktivní prodejce "' + ISNULL(@customerName, '?') + N'" je nastaven jako kamenná prodejna, ale chybí adresa prodejny. Nutno doplnit v CRM systému';
		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Chybějící adresa prodejny', @code, @message;

		DELETE FROM @isu WHERE customerId = @customerId;
	 END
END
GO


