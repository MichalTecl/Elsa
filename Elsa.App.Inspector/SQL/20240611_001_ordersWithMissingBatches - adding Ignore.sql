﻿IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'insp_sentOrdersWithoutBatches')
	DROP PROCEDURE insp_sentOrdersWithoutBatches;

GO

CREATE PROCEDURE [dbo].[insp_sentOrdersWithoutBatches] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	DECLARE @retryOrderId INT = (SELECT TOP 1 ida.IntValue
	                               FROM InspectionIssue isu
								   JOIN InspectionIssueData ida ON (ida.IssueId = isu.Id)
								  WHERE isu.Id = @retryIssueId
								    AND ida.PropertyName = 'OrderId');
			
    DECLARE @ordids TABLE (id INT);
	INSERT INTO @ordids
	SELECT DISTINCT po.Id
	  FROM PurchaseOrder po
	  INNER JOIN vwOrderItems oi ON (po.Id = oi.OrderId)
	  INNER JOIN OrderItem oitm ON (oi.OrderItemId = oitm.Id)
	  INNER JOIN VirtualProductOrderItemMapping oim ON (oim.ProjectId = @projectId AND oim.ItemName = oitm.PlacedName)
	  INNER JOIN VirtualProductMaterial vp ON (oim.VirtualProductId = vp.VirtualProductId)
	  INNER JOIN Material m ON (vp.ComponentId = m.Id)
	 WHERE 1=1
	   AND po.ProjectId = @projectId
	   AND ((@retryOrderId IS NULL) OR (po.Id = @retryOrderId))
	   AND po.BuyDate > (GETDATE() - 100)
	   AND po.OrderStatusId = 5
	   AND oitm.PlacedName NOT IN (SELECT ItemName FROM KitDefinition WHERE ProjectId = @projectId)
	   AND ISNULL(m.CanBeDigitalOnly, 0) = 0 
	   AND NOT EXISTS(SELECT TOP 1 1 
	                    FROM OrderItemMaterialBatch oimb
					   WHERE oimb.OrderItemId = oi.OrderItemId);

     WHILE(EXISTS(SELECT TOP 1 1 FROM @ordids))
	 BEGIN
		DECLARE @code NVARCHAR(100);
		DECLARE @message NVARCHAR(2000);
		DECLARE @ordid INT;
		
		SELECT TOP 1 @ordid = o.Id,
		             @code = 'MissingBatch_order=' + LTRIM(STR(o.Id)),
					 @message = N'Objednávka č. ' + po.OrderNumber + ' je odeslána bez přiřazených šarží'
					 FROM PurchaseOrder po
					 JOIN @ordids o ON (po.id = o.id);

		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Objednávky bez přiřazených šarží', @code, @message;

		EXEC inspfw_setIssueDataInt @issueId, 'OrderId', @ordid;

		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneDay.html', N'Odložit na zítra';
		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/Ignore.html', N'Ignorovat';

		DELETE FROM @ordids 
		 WHERE Id = @ordid;
	 END

END