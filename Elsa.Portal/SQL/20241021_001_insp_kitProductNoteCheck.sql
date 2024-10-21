IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_kitProductNoteCheck')
	DROP PROCEDURE insp_kitProductNoteCheck;

GO

CREATE PROCEDURE insp_kitProductNoteCheck (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	/*
	const string issueTypeColumn = "IssueType";
	const string issueCodeColumn = "IssueCode";
	const string messageColumn = "Message";
	const string issueDataPrefix = "data:";
	const string actionControlPrefix = "ActionControlUrl";
	const string actionNamePrefix = "ActionName";
	*/
    
	-- DECLARE @projectId INT = 1

DECLARE @cuts TABLE (
        Id INT, 
        OrderId BIGINT, 
        KitNr INT, 
        KitName NVARCHAR(1000), 
        ItemType NVARCHAR(1000), 
        Item NVARCHAR(1000),
		KitDefinitionId INT,
		SelectionGroupId INT,
		SelectionGroupItemId INT
    );

insert into @cuts
exec ParseKitNote @projectId;


SELECT N'Chybná poznámka sady' IssueType,
       N'invalidcustnotekit_' + LTRIM(STR(y.OrderId)) IssueCode,
	   MIN(N'Objednávka ' + y.OrderNumber + ' (' + y.CustomerName + N') nemá správnou poznámku s položkami sady: ' + y.Err) Message,	  
	   '/UI/Inspector/ActionControls/OrderPacking/OrderNoteDetailView.html' "ActionControlUrl_Preview",
	   N'Detail' "ActionName_Preview",
		y.OrderId "data:orderId",
		MIN(y.CustomerNote) "data:customerNote"
  FROM (
	SELECT CASE 	     
			 WHEN (x.KitNr IS NULL) THEN N'Text chybí nebo je nekompletní'
			 WHEN (x.KitDefinitionId IS NULL)  THEN N'Text "' + ISNULL(x.KitName, '') +'" není známý název sady'
			 WHEN (x.SelectionGroupId IS NULL) THEN N'Text "' + ISNULL(x.ItemType, '') +'" není známý název položky sady'
			 WHEN (x.SelectionGroupItemId IS NULL) THEN N'Text "' + ISNULL(x.Item, '') +'" není známý název položky sady'
			 WHEN (x.countInNote <> x.OrderItemQty) THEN N'Počet vyplněných sad neodpovídá objednanému množství'
			 WHEN (x.NumberOfItemsInNote <> x.NumberOfItemsRequired) THEN N'Sada "' + x.KitName + N'" nemá správně vyplněny položky'
		  END AS Err,
		  x.*

	FROM (
		select c.*, 
		   noteKitCount.c countInNote, -- kolikrat je tento kit v poznamce (1. sada, 2. sada)
		   oi.Quantity OrderItemQty, -- Kolikrat je tento kit v kosiku 
		   noteKitItemsCount.c NumberOfItemsInNote, -- Kolik polozek ma celkem tento kit v poznamce
		   rqKitItemsCount.c * ISNULL(oi.Quantity, 1) NumberOfItemsRequired,
		   po.OrderNumber,
		   po.CustomerName,
		   po.CustomerNote
		 FROM @cuts c
		 LEFT JOIN (SELECT OrderId, KitName, COUNT(DISTINCT KitNr) c
					  FROM @cuts
					 GROUP BY OrderId, KitName) noteKitCount ON (noteKitCount.OrderId = c.OrderId AND noteKitCount.KitName = c.KitName)
		 LEFT JOIN OrderItem oi ON (oi.PurchaseOrderId = c.OrderId AND oi.PlacedName = c.KitName)
		 LEFT JOIN (SELECT z.OrderId, z.KitDefinitionId, COUNT(z.SelectionGroupItemId) c
					  FROM @cuts z
					 GROUP BY z.OrderId, KitDefinitionId) noteKitItemsCount ON (noteKitItemsCount.OrderId = c.OrderId AND noteKitItemsCount.KitDefinitionId = c.KitDefinitionId) 
		 LEFT JOIN (SELECT ksg.KitDefinitionId, COUNT(DISTINCT ksg.Id) c
					  FROM KitSelectionGroup ksg	
					  JOIN (SELECT KitSelectionGroupId, COUNT(DISTINCT Id) c
							 FROM KitSelectionGroupItem 
							GROUP BY KitSelectionGroupId
							HAVING COUNT(DISTINCT Id) > 1) x ON (x.KitSelectionGroupId = ksg.Id)
					 GROUP BY ksg.KitDefinitionId
			 
					 ) rqKitItemsCount ON (rqKitItemsCount.KitDefinitionId = c.KitDefinitionId)
		 LEFT JOIN PurchaseOrder po ON (po.Id = c.OrderId)
	) x  
) y 
WHERE y.Err is not null
GROUP BY y.orderid;
END
GO


