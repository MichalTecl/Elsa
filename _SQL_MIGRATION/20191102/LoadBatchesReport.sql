ALTER PROCEDURE [dbo].[LoadBatchesReport] (
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
	@compositionId NVARCHAR(100) = NULL,
	@componentId   NVARCHAR(100) = NULL,
	@batchId NVARCHAR(100) = NULL,
	@orderId BIGINT = NULL,
	@onlyBlocking BIT = NULL)
AS
BEGIN
	
	IF (@orderNumber IS NOT NULL)
	BEGIN
		SELECT TOP 1 @orderId = Id 
		  FROM PurchaseOrder po
		 WHERE po.OrderNumber = @orderNumber;
	END
		
	DECLARE @keys TABLE (BatchNumber NVARCHAR(64), MaterialId INT);

	INSERT INTO @keys
	SELECT x.BatchNumber, x.MaterialId
	FROM 
	(
		SELECT DISTINCT b.BatchNumber, b.MaterialId, MAX(b.Created) ctd
		FROM MaterialBatch b
		INNER JOIN Material m ON (b.MaterialId = m.Id)
		WHERE b.ProjectId = @projectId
		  AND ((@materialId IS NULL) OR (b.MaterialId = @materialId))
		  AND ((@batchNumber IS NULL) OR (b.BatchNumber = @batchNumber))
		  AND ((@dtFrom IS NULL) OR (b.Created >= @dtFrom))
		  AND ((@dtTo IS NULL) OR (b.Created <= @dtTo))
		  AND ((@closed IS NULL) OR ((@closed = 1) AND (b.CloseDt IS NOT NULL)) OR ((@closed = 0) AND (b.CloseDt IS NULL)))
		  AND ((@locked IS NULL) OR ((@locked = 1) AND (b.LockDt IS NOT NULL)) OR ((@locked = 0) AND (b.LockDt IS NULL)))
		  AND ((@inventoryTypeId IS NULL) OR (m.InventoryId = @inventoryTypeId))
		  AND ((@onlyProduced IS NULL) OR (@onlyProduced = 0) OR (EXISTS(SELECT TOP 1 1 FROM MaterialComposition mc WHERE mc.CompositionId = m.Id)))
		  AND ((@onlyBought IS NULL) OR (@onlyBought = 0) OR (NOT EXISTS(SELECT TOP 1 1 FROM MaterialComposition mc WHERE mc.CompositionId = m.Id)))
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

		  AND ((ISNULL(@onlyBlocking, 0) = 0) OR (EXISTS(SELECT TOP 1 1 
														 FROM PurchaseOrder spo
														 JOIN OrderItem soi ON (spo.Id = dbo.GetOrderItemOrderId(soi.Id))
														 JOIN OrderItemMaterialBatch soimb ON (soimb.OrderItemId = soi.Id)
														WHERE spo.OrderStatusId <> 5
														  AND soimb.MaterialBatchId = b.Id)))	

		  AND ((@batchId IS NULL) OR EXISTS(SELECT TOP 1 1 FROM ParseBatchKey(@batchId) pbk WHERE pbk.BatchNumber = b.BatchNumber AND pbk.MaterialId = b.MaterialId))

		  AND ((@compositionId IS NULL) OR (b.Id IN (select mbc.ComponentId 
														from MaterialBatch composition
														join MaterialBatchComposition mbc ON (mbc.CompositionId = composition.Id)
														 where composition.Calculatedkey = @compositionId)))

		AND ((@componentId IS NULL) OR (b.Id IN (select mbc.CompositionId
													from MaterialBatch component
													join MaterialBatchComposition mbc ON (mbc.ComponentId = component.Id)
													 where component.Calculatedkey = @componentId)))
		GROUP BY b.BatchNumber, b.MaterialId
		ORDER BY MAX(b.Created) DESC
		OFFSET @pageSize * @pageNumber ROWS
		FETCH NEXT @pageSize ROWS ONLY
		) x;

    SELECT   b.CalculatedKey  BatchId, 
			 i.Name          InventoryName,
	         b.BatchNumber   BatchNumber, 
			 m.Name          MaterialName, 
			 m.Id            MaterialId,  
			 SUM(convertedVolume.Value) BatchVolume,
			 u.Symbol        Unit,
			 MAX(b.Created)       BatchCreateDt,
			 MAX(b.CloseDt)       BatchCloseDt,
			 MAX(b.LockDt)        BatchLockDt,
			 CAST(MIN(CAST(b.IsAvailable AS INT)) AS BIT)   BatchAvailable,
			 MAX(b.Produced)      BatchProductionDt,
			 CAST(MIN(CAST(b.AllStepsDone AS INT)) AS BIT)  BatchStepsDone,			 
			 SUM(ISNULL(components.cnt, 0) + ISNULL(stepComponents.cnt, 0))    NumberOfComponents,
			 SUM(ISNULL(compositions.cnt, 0) + ISNULL(stepCompositions.cnt, 0))  NumberOfCompositions,
			 SUM(ISNULL(materialSteps.cnt, 0)) NumberOfRequiredSteps,
			 SUM(ISNULL(orders.cnt, 0))           NumberOfOrders,
			 SUM(b.Price)         BatchPrice,
			 STRING_AGG(b.InvoiceNr, ';')     InvoiceNumber,
			 SUM(ISNULL(stockEvents.cnt, 0)) NumberOfStockEvents
        FROM MaterialBatch     b
  INNER JOIN @keys             k ON (k.BatchNumber = b.BatchNumber AND k.MaterialId = b.MaterialId) 
  INNER JOIN Material          m ON (b.MaterialId = m.Id)
  INNER JOIN MaterialInventory i ON (m.InventoryId = i.Id)
  
  CROSS APPLY ConvertToSmallestUnit(b.Volume, b.UnitId) convertedVolume
    
  INNER JOIN MaterialUnit      u ON (convertedVolume.TargetUnitId = u.Id)
  
  
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
  
  GROUP BY b.CalculatedKey, 
	       i.Name,
		   b.BatchNumber,
		   m.Name,
		   m.Id,
		   u.Symbol
  ORDER BY MAX(b.Created) DESC;

END


GO


