IF EXISTS(SELECT * FROM sys.tables WHERE name='OrderStatusWarnAge')
	DROP TABLE OrderStatusWarnAge;

GO

IF NOT EXISTS(SELECT * FROM sys.tables WHERE name='ErpOrderStatusWarnAge')
BEGIN
	CREATE TABLE ErpOrderStatusWarnAge (ProjectId INT NOT NULL, ErpOrderStatusId NVARCHAR(64) NOT NULL, MaxDaysBeforeWarn INT NOT NULL); 

	INSERT INTO ErpOrderStatusWarnAge (ProjectId, ErpOrderStatusId, MaxDaysBeforeWarn)
	SELECT 1, '1', 11 UNION -- Neuhrazena
	SELECT 1, '5', 7 UNION -- Uhrazena (ERP = Uhrazeno)
	SELECT 1, '45', 7 UNION -- Uhrazena (ERP = Comgate - zaplaceno)
	SELECT 1, '46', 3 UNION -- CG nezaplaceno 
	SELECT 1, '44', 10; --  Comgate - čeká se

END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'insp_ordersStuckInStatus')
	DROP PROCEDURE insp_ordersStuckInStatus;

GO

CREATE PROCEDURE insp_ordersStuckInStatus (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	DECLARE @retryOrderId INT = (SELECT TOP 1 ida.IntValue
	                               FROM InspectionIssue isu
								   JOIN InspectionIssueData ida ON (ida.IssueId = isu.Id)
								  WHERE isu.Id = @retryIssueId
								    AND ida.PropertyName = 'OrderId');
			
    DECLARE @ordids TABLE (id INT);

	INSERT INTO @ordids
	SELECT po.Id 
	  FROM PurchaseOrder po
	  JOIN ErpOrderStatusWarnAge owa ON (po.ErpStatusId = owa.ErpOrderStatusId)
	  JOIN (SELECT poh.Id OrderId, poh.ErpStatusId, MIN(poh.AuditDate) SetDt
			  FROM PurchaseOrderHistory poh
		  GROUP BY poh.Id, poh.ErpStatusId) statusSet ON (    po.Id = statusSet.OrderId 
														  AND po.ErpStatusId = statusSet.ErpStatusId)  
	WHERE po.ProjectId = @projectId
	  AND ((@retryOrderId IS NULL) OR (@retryOrderId = po.Id))
	  AND po.BuyDate > (GETDATE() - 90)
	  AND DATEDIFF(day, statusSet.SetDt, GETDATE()) >= owa.MaxDaysBeforeWarn;
	 	 		
     WHILE(EXISTS(SELECT TOP 1 1 FROM @ordids))
	 BEGIN
		DECLARE @code NVARCHAR(100);
		DECLARE @message NVARCHAR(2000);
		DECLARE @ordid INT = (SELECT TOP 1 Id FROM @ordids);
		DELETE FROM @ordids WHERE Id = @ordid;
	
		SELECT @code = 'stuckInErpStat_' + LTRIM(STR(@ordid)),
		       @message = N'Objednávka ' + po.OrderNumber + ' je ve stavu "' + po.ErpStatusName + '" více než ' + LTRIM(STR(owa.MaxDaysBeforeWarn)) + N' dnů' 
		  FROM PurchaseOrder po
		  JOIN ErpOrderStatusWarnAge owa ON (po.ErpStatusId = owa.ErpOrderStatusId)
		 WHERE po.Id = @ordid;

		 --SELECT @ordid, @code, @message;
						
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Nevyřízené objednávky', @code, @message;

		EXEC inspfw_setIssueDataInt @issueId, 'OrderId', @ordid;

		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneDay.html', N'Odložit na zítra';
		

		
	 END

END





