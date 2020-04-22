IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'vwBatchEvent')
BEGIN
	DROP VIEW vwBatchEvent;
END

GO

CREATE VIEW vwBatchEvent
AS
-- Batch creation
SELECT mb.Id BatchId, mb.Created EventDt, mb.Volume Delta, mb.UnitId DeltaUnitId, 'BATCH_CREATION' EventName FROM MaterialBatch mb
UNION ALL
-- Stock Events      
SELECT mse.BatchId, mse.EventDt, -1 * mse.Delta, mse.UnitId DetlaUnitId, 'STOCK_EVENT' EventName FROM MaterialStockEvent mse
UNION ALL
-- Sale event allocations
SELECT sea.BatchId, sea.AllocationDt, -1 * sea.AllocatedQuantity Delta, sea.UnitId DeltaUnitId, 'DIRECT_SALE_ALLOCATION' EventName   FROM SaleEventAllocation sea
-- Sale event returns
UNION ALL
SELECT sea.BatchId, sea.ReturnDt, sea.ReturnedQuantity Delta, sea.UnitId DeltaUnitId, 'DIRECT_SALE_RETURN' EventName   FROM SaleEventAllocation sea
UNION ALL
-- Used in compositions
SELECT mbc.ComponentId BatchId, composition.Created EventDt, -1 * mbc.Volume Delta, mbc.UnitId DeltaUnitId, 'USED_AS_COMPONENT'
  FROM MaterialBatchComposition mbc
  JOIN MaterialBatch composition ON (composition.Id = mbc.CompositionId)
UNION ALL
-- Sold via eshop
SELECT oimb.MaterialBatchId BatchId, oimb.AssignmentDt EventDt,(CASE WHEN po.OrderStatusId = 7 OR po.OrderStatusId = 8 THEN 0 ELSE -1 * oimb.Quantity END) Delta, m.NominalUnitId DeltaUnitId, 'ESHOP_SALE' EventName 
  FROM OrderItemMaterialBatch oimb
  JOIN MaterialBatch mb ON (oimb.MaterialBatchId = mb.Id)
  JOIN Material m ON (mb.MaterialId = m.Id)
  JOIN OrderItem oi ON (oi.Id = oimb.OrderItemId)
  LEFT JOIN OrderItem kitParent ON (oi.KitParentId = kitParent.Id)
  JOIN PurchaseOrder po ON (po.Id = ISNULL(kitParent.PurchaseOrderId, oi.PurchaseOrderId));