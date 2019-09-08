IF EXISTS(SELECT TOP 1 1 FROM sys.procedures where name = 'LoadBatchesReport')
BEGIN
	DROP PROCEDURE LoadBatchesReport;
END
 
GO

CREATE PROCEDURE [dbo].[LoadBatchesReport] (
	@projectId INT,
    @pageSize INT,
    @pageNumber INT,
    @materialId INT = NULL,	
    @orderNumber NVARCHAR(1000) = NULL,
    @batchNumber NVARCHAR(1000) = NULL,
    @dtFrom DATETIME = NULL,
    @dtTo DATETIME = NULL,
    @closed BIT = NULL,
    @locked BIT = NULL,
    @inventoryTypeId INT = NULL,
	@onlyProduced BIT = NULL,
	@onlyBought BIT = NULL,
	@compositionId INT = NULL,
	@componentId   INT = NULL,
	@batchId INT = NULL,
	@orderId BIGINT = NULL,
	@onlyBlocking BIT = NULL)
AS
BEGIN

    SELECT b.Id            BatchId, 
			 i.Name          InventoryName,
	         b.BatchNumber   BatchNumber, 
			 m.Name          MaterialName, 
			 m.Id            MaterialId,  
			 b.Volume        BatchVolume,
			 u.Symbol        Unit,
			 b.Created       BatchCreateDt,
			 b.CloseDt       BatchCloseDt,
			 b.LockDt        BatchLockDt,
			 b.IsAvailable   BatchAvailable,
			 b.Produced      BatchProductionDt,
			 b.AllStepsDone  BatchStepsDone,			 
			 ISNULL(components.cnt, 0) + ISNULL(stepComponents.cnt, 0)    NumberOfComponents,
			 ISNULL(compositions.cnt, 0) + ISNULL(stepCompositions.cnt, 0)  NumberOfCompositions,
			 ISNULL(materialSteps.cnt, 0) NumberOfRequiredSteps,
			 ISNULL(orders.cnt, 0)           NumberOfOrders,
			 b.Price         BatchPrice,
			 b.InvoiceNr     InvoiceNumber,
			 ISNULL(stockEvents.cnt, 0) NumberOfStockEvents
        FROM MaterialBatch     b
  INNER JOIN Material          m ON (b.MaterialId = m.Id)
  INNER JOIN MaterialInventory i ON (m.InventoryId = i.Id)
  INNER JOIN MaterialUnit      u ON (b.UnitId = u.Id)
  
  LEFT JOIN  (SELECT mbc.CompositionId, COUNT(mbc.Id) as cnt
                FROM MaterialBatchComposition mbc
			GROUP BY mbc.CompositionId) components ON (components.CompositionId = b.Id)
			  	  
  LEFT JOIN (SELECT bps.BatchId, COUNT(DISTINCT ssb.SourceBatchId) as cnt
			   FROM       BatchProductionStep bps
         INNER JOIN BatchProuctionStepSourceBatch ssb ON (ssb.StepId = bps.Id)
           GROUP BY bps.BatchId) stepComponents ON (stepComponents.BatchId = b.Id)
		   
  LEFT JOIN  (SELECT mbc.ComponentId, COUNT(mbc.Id) as cnt
                FROM MaterialBatchComposition mbc
			GROUP BY mbc.ComponentId) compositions ON (compositions.ComponentId = b.Id)  
  
  LEFT JOIN  (select SourceBatchId, COUNT(DISTINCT BatchId) CNT
			    from BatchProuctionStepSourceBatch ssb
		  inner join BatchProductionStep           bps ON (ssb.StepId = bps.Id)
			GROUP BY SourceBatchId) stepCompositions ON (stepCompositions.SourceBatchId = b.Id)

  LEFT JOIN  (SELECT COALESCE(oimb.MaterialBatchId, coimb.MaterialBatchId, -1) BatchId, COUNT(DISTINCT oi.PurchaseOrderId) as cnt
                FROM OrderItem oi
				LEFT JOIN OrderItemMaterialBatch oimb ON (oimb.OrderItemId = oi.Id)
				LEFT JOIN OrderItem ci ON (oi.Id = ci.KitParentId)			   
				LEFT JOIN OrderItemMaterialBatch coimb ON (coimb.OrderItemId = ci.Id)
			GROUP BY COALESCE(oimb.MaterialBatchId, coimb.MaterialBatchId, -1)) orders ON (orders.BatchId = b.Id)

  LEFT JOIN (SELECT ms.MaterialId, COUNT(ms.Id) as cnt
               FROM MaterialProductionStep ms
			 GROUP BY ms.MaterialId) materialSteps ON (materialSteps.MaterialId = b.MaterialId)
  
  LEFT JOIN (SELECT se.BatchId, COUNT(se.Id) as cnt
               FROM MaterialStockEvent se
		   GROUP BY se.BatchId)      stockEvents ON (stockEvents.BatchId = b.Id)
   

 WHERE b.ProjectId = @projectId
   AND m.ProjectId = @projectId
   
   AND ((@batchId IS NULL) OR (b.Id = @batchId))
   AND ((@materialId IS NULL) OR (b.MaterialId = @materialId))
   AND ((@batchNumber IS NULL) OR (b.BatchNumber LIKE @batchNumber))
   AND ((@dtFrom IS NULL) OR (b.Created >= @dtFrom))
   AND ((@dtTo IS NULL) OR (b.Created <= @dtTo))
   AND ((@inventoryTypeId IS NULL) OR (m.InventoryId = @inventoryTypeId))
   AND ((@closed IS NULL) OR ((@closed = 1) AND (b.CloseDt IS NOT NULL)) OR ((@closed = 0) AND (b.CloseDt IS NULL)))
   AND ((@locked IS NULL) OR ((@locked = 1) AND (b.LockDt IS NOT NULL)) OR ((@locked = 0) AND (b.LockDt IS NULL)))

   AND ((@orderNumber IS NULL) OR (b.Id IN 
											(SELECT oimb.MaterialBatchId
											  FROM PurchaseOrder po
											  INNER JOIN OrderItem oi ON (oi.PurchaseOrderId = po.Id)
											  INNER JOIN OrderItemMaterialBatch oimb ON (oimb.OrderItemId = oi.Id)
											  WHERE po.OrderNumber = @orderNumber
												AND oimb.MaterialBatchId IS NOT NULL
											 UNION
											  SELECT cib.MaterialBatchId
											  FROM PurchaseOrder po
											  INNER JOIN OrderItem oi ON (oi.PurchaseOrderId = po.Id)
											  INNER JOIN OrderItem ci ON (oi.Id = ci.KitParentId)
											  INNER JOIN OrderItemMaterialBatch cib ON (cib.OrderItemId = ci.Id)
											  WHERE po.OrderNumber = @orderNumber
												AND cib.MaterialBatchId IS NOT NULL)))
   AND ((@orderId IS NULL) OR (b.Id IN 
											(SELECT oimb.MaterialBatchId
											  FROM PurchaseOrder po
											  INNER JOIN OrderItem oi ON (oi.PurchaseOrderId = po.Id)
											  INNER JOIN OrderItemMaterialBatch oimb ON (oimb.OrderItemId = oi.Id)
											  WHERE po.Id = @orderId
												AND oimb.MaterialBatchId IS NOT NULL
											 UNION
											  SELECT cib.MaterialBatchId
											  FROM PurchaseOrder po
											  INNER JOIN OrderItem oi ON (oi.PurchaseOrderId = po.Id)
											  INNER JOIN OrderItem ci ON (oi.Id = ci.KitParentId)
											  INNER JOIN OrderItemMaterialBatch cib ON (cib.OrderItemId = ci.Id)
											  WHERE po.Id = @orderId
												AND cib.MaterialBatchId IS NOT NULL)))

   AND ((@onlyProduced IS NULL) OR (@onlyProduced = 0) OR (EXISTS(SELECT TOP 1 1 FROM MaterialComposition mc WHERE mc.CompositionId = m.Id)))

   AND ((@onlyBought IS NULL) OR (@onlyBought = 0) OR (NOT EXISTS(SELECT TOP 1 1 FROM MaterialComposition mc WHERE mc.CompositionId = m.Id)))

   AND ((@compositionId IS NULL) OR (b.Id IN (SELECT mbc.ComponentId FROM MaterialBatchComposition mbc WHERE mbc.CompositionId = @compositionId)) 
                                 OR (b.Id IN (SELECT bss.SourceBatchId 
								                FROM BatchProductionStep bps 
                                          INNER JOIN BatchProuctionStepSourceBatch bss ON (bss.StepId = bps.Id)
										   	  WHERE bps.BatchId = @compositionId)))

   AND ((@componentId IS NULL) OR (b.Id IN (SELECT mbc.CompositionId FROM MaterialBatchComposition mbc WHERE mbc.ComponentId = @componentId))
                               OR (b.Id IN (SELECT bps.BatchId 
								                FROM BatchProductionStep bps 
                                          INNER JOIN BatchProuctionStepSourceBatch bss ON (bss.StepId = bps.Id)
										   	  WHERE bss.SourceBatchId = @componentId)))

	AND ((ISNULL(@onlyBlocking, 0) = 0) OR (EXISTS(SELECT TOP 1 1 
	                                                 FROM PurchaseOrder spo
													 JOIN OrderItem soi ON (spo.Id = dbo.GetOrderItemOrderId(soi.Id))
													 JOIN OrderItemMaterialBatch soimb ON (soimb.OrderItemId = soi.Id)
													WHERE spo.OrderStatusId <> 5
													  AND soimb.MaterialBatchId = b.Id)))
      
   ORDER BY b.Created DESC
   OFFSET @pageSize * @pageNumber ROWS
   FETCH NEXT @pageSize ROWS ONLY	

END



GO


