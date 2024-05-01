IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_missingEshopOrStoreInfo')
	DROP PROCEDURE insp_missingEshopOrStoreInfo;

GO

CREATE PROCEDURE [dbo].[insp_missingEshopOrStoreInfo] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	DECLARE @isu TABLE (customerId INT, customerName NVARCHAR(255));
	INSERT INTO @isu
	SELECT c.Id, c.Name
	  FROM Customer c
	  WHERE c.ProjectId = @projectId
		AND c.IsValuableDistributor = 1
		AND ISNULL(c.HasEshop, 0) = 0
		AND ISNULL(c.HasStore, 0) = 0;
		   	   		    	
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
	
		SET @code = 'missingShopTypeInfo_' + LTRIM(STR(@customerId));
		SET @message = N'Aktivní prodejce "' + ISNULL(@customerName, '?') + N'" nemá vyplněnu informaci E-Shop/Kamenný obchod. Nutno vyplnit v CRM systému';
		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Chybějící typ obchodu', @code, @message;

		DELETE FROM @isu WHERE customerId = @customerId;
	 END
END
GO


