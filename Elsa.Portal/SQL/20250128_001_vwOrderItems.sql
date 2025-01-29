IF EXISTS(SELECT TOP 1 1 FROM sys.views WHERE name = 'vwOrderItems')
	DROP VIEW vwOrderItems;

GO

CREATE VIEW vwOrderItems
AS
SELECT po.Id OrderId, oi.Id OrderItemId, 0 IsKitItem
  FROM PurchaseOrder po
  INNER JOIN OrderItem oi ON (po.Id = oi.PurchaseOrderId)
UNION
  SELECT po.Id OrderId, oi.Id OrderItemId, 1 IsKitItem
  FROM PurchaseOrder po
  INNER JOIN OrderItem poi ON (po.Id = poi.PurchaseOrderId)
  INNER JOIN OrderItem oi ON (poi.Id = oi.KitParentId);

GO