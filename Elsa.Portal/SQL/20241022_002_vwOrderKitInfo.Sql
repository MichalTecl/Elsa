IF EXISTS(SELECT TOP 1 1 FROM sys.views WHERE name = 'vwOrderKitInfo')
	DROP VIEW vwOrderKitInfo;

GO

CREATE VIEW vwOrderKitInfo
AS
SELECT po.Id OrderId,        
	   oi.Quantity,
	   oi.Id OrderItemId,
	   kd.Id KitDefinitionId,
	   CASE WHEN MAX(options.c) > 1 THEN 1 ELSE 0 END RequiresSelection
  FROM PurchaseOrder po
  JOIN OrderItem     oi ON (oi.PurchaseOrderId = po.Id)
  JOIN KitDefinition kd ON (kd.ItemName = oi.PlacedName)
  JOIN KitSelectionGroup ksg ON (ksg.KitDefinitionId = kd.Id)
  JOIN (SELECT ki.KitSelectionGroupId, COUNT(ki.Id) c
          FROM KitSelectionGroupItem ki
		GROUP BY ki.KitSelectionGroupId) options ON (options.KitSelectionGroupId = ksg.Id)
GROUP BY po.Id,        
	   oi.Quantity,
	   oi.Id,
	   kd.Id;