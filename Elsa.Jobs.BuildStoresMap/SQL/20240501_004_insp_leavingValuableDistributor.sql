IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_leavingValuableDistributor')
	DROP PROCEDURE insp_leavingValuableDistributor;

GO

CREATE PROCEDURE [dbo].[insp_leavingValuableDistributor] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	DECLARE @months INT;
	SELECT TOP 1 @months = CAST(ValueJson AS INT) - 1 FROM SysConfig WHERE ProjectId = @projectId AND [Key] = 'ValuableDistributor.MaxMonthsFromLastOrder';
	IF (ISNULL(@months, 0) < 1)
		RETURN;
	
	DECLARE @isu TABLE (customerId INT, customerName NVARCHAR(255), lastOrderDt NVARCHAR(255));
	INSERT INTO @isu
	SELECT c.Id, c.Name, FORMAT(clo.LatestSuccessOrderDt, 'dd.MM.yyyy')
	  FROM Customer c
	  JOIN vwCustomerLatestOrder clo ON (clo.CustomerId = c.Id) 
	 WHERE c.ProjectId = @projectId
	   AND c.IsValuableDistributor = 1
	   AND DATEADD(month, @months, clo.LatestSuccessOrderDt) < GETDATE();
				   		   	   		    	
     WHILE(EXISTS(SELECT TOP 1 1 FROM @isu))
	 BEGIN
		DECLARE @customerId INT,
				@customerName NVARCHAR(255),
				@lastOrder NVARCHAR(255);

		SELECT TOP 1
			@customerId = customerId,
			@customerName = customerName,
			@lastOrder = lastOrderDt
		FROM
			@isu;

		DECLARE @code NVARCHAR(200);
		DECLARE @message NVARCHAR(2000);
			
		SET @code = 'leavingValDist_' + @lastOrder + '.' + LTRIM(STR(@customerId));
		SET @message = N'Aktivní prodejce "' + ISNULL(@customerName, '?') + N'" neprovedl žádnou objednávku od ' + @lastOrder;
		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Aktivní prodejce dlouho neobjednal', @code, @message;

		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneWeek.html', N'Odložit o týden';
		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneMonth.html', N'Odložit o měsíc';

		DELETE FROM @isu WHERE customerId = @customerId;
	 END
END
GO


