IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'vwORderItems')
BEGIN
	DROP VIEW vwOrderItems;
END

GO

CREATE VIEW vwOrderItems
AS
SELECT po.Id OrderId, oi.Id OrderItemId
  FROM PurchaseOrder po
  INNER JOIN OrderItem oi ON (po.Id = oi.PurchaseOrderId)
UNION
  SELECT po.Id OrderId, oi.Id OrderItemId
  FROM PurchaseOrder po
  INNER JOIN OrderItem poi ON (po.Id = poi.PurchaseOrderId)
  INNER JOIN OrderItem oi ON (poi.Id = oi.KitParentId);