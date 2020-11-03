
IF NOT EXISTS(SELECT * FROM sys.tables WHERE name='OrderStatusWarnAge')
BEGIN
	CREATE TABLE OrderStatusWarnAge (ProjectId INT NOT NULL, OrderStatusId INT NOT NULL, MaxDaysBeforeWarn INT NOT NULL); 

	INSERT INTO OrderStatusWarnAge (ProjectId, OrderStatusId, MaxDaysBeforeWarn)
	SELECT 1, 1, 30 UNION
	SELECT 1, 2, 30 UNION
	SELECT 1, 3, 10 UNION
	SELECT 1, 4, 10; 

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
	FROM
		(SELECT poh.Id, poh.OrderStatusId, MIN(poh.AuditDate) SetDt
		  FROM PurchaseOrderHistory poh
		 WHERE poh.ProjectId = @projectId
		   AND ((@retryOrderId IS NULL) OR (poh.Id = @retryOrderId))
		   AND poh.AuditDate > (GETDATE() - 365)
		 GROUP BY poh.Id, poh.OrderStatusId) statChanges
	   INNER JOIN PurchaseOrder po ON (po.Id = statChanges.Id AND po.OrderStatusId = statChanges.OrderStatusId)
	   INNER JOIN OrderStatusWarnAge owa ON (statChanges.OrderStatusId = owa.OrderStatusId)
	 WHERE owa.MaxDaysBeforeWarn > DATEDIFF(day, statChanges.SetDt, GETDATE());
	
     WHILE(EXISTS(SELECT TOP 1 1 FROM @ordids))
	 BEGIN
		DECLARE @code NVARCHAR(100);
		DECLARE @message NVARCHAR(2000);
		DECLARE @ordid INT = (SELECT TOP 1 Id FROM @ordids);
	
		SELECT @code = 'stuckInStat_' + LTRIM(STR(@ordid)),
		       @message = N'Objednávka ' + po.OrderNumber + ' je ve stavu "' + po.ErpStatusName + '" více než ' + LTRIM(STR(owa.MaxDaysBeforeWarn)) + ' dnů' 
		  FROM PurchaseOrder po
		  JOIN OrderStatusWarnAge owa ON (po.OrderStatusId = owa.OrderStatusId)
		 WHERE po.Id = @ordid;
		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Nevyřízené objednávky', @code, @message;

		EXEC inspfw_setIssueDataInt @issueId, 'OrderId', @ordid;

		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneDay.html', N'Odložit na zítra';
		

		DELETE FROM @ordids 
		 WHERE Id = @ordid;
	 END

END