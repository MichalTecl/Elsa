USE [test]
GO

/****** Object:  StoredProcedure [dbo].[LoadBatchesReport]    Script Date: 12/21/2019 11:44:49 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


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
	@onlyBlocking BIT = NULL,
	@segmentId INT = NULL)
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
		SELECT b.BatchNumber, b.MaterialId, MAX(b.Created) ctd
		FROM MaterialBatch b
		INNER JOIN Material m ON (b.MaterialId = m.Id)
		WHERE b.ProjectId = @projectId
		  AND ((@segmentId IS NULL) OR (b.Id = @segmentId))
		  AND ((@materialId IS NULL) OR (b.MaterialId = @materialId))
		  AND ((@batchNumber IS NULL) OR (b.BatchNumber = @batchNumber))
		  AND ((@dtFrom IS NULL) OR (b.Created >= @dtFrom))
		  AND ((@dtTo IS NULL) OR (b.Created <= @dtTo))
		  AND ((@closed IS NULL) OR ((@closed = 1) AND (b.CloseDt IS NOT NULL)) OR ((@closed = 0) AND (b.CloseDt IS NULL)))
		  AND ((@locked IS NULL) OR ((@locked = 1) AND (b.LockDt IS NOT NULL)) OR ((@locked = 0) AND (b.LockDt IS NULL)))
		  AND ((@inventoryTypeId IS NULL) OR (m.InventoryId = @inventoryTypeId))
		  AND ((@onlyProduced IS NULL) OR (@onlyProduced = 0) OR (EXISTS(SELECT TOP 1 1 FROM Recipe r WHERE r.ProducedMaterialId = m.Id)))
		  AND ((@onlyBought IS NULL) OR (@onlyBought = 0) OR (NOT EXISTS(SELECT TOP 1 1 FROM Recipe r WHERE r.ProducedMaterialId = m.Id)))
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

	SELECT y.BatchId,
		   y.InventoryName,
		   y.BatchNumber,
		   y.MaterialName,
		   y.MaterialId,
		   y.BatchVolume,
		   y.Unit,
		   y.BatchCreateDt,
		   y.BatchCloseDt,
		   y.BatchLockDt,
		   y.BatchAvailable,
		   y.BatchProductionDt,
		   y.BatchStepsDone,
		   ISNULL(numberOfComponents.cnt, 0),
		   ISNULL(numberOfCompositions.cnt, 0),
		   0 as numberOfMaterialSteps, 		   		   
		   ISNULL(numberOfOrders.cnt, 0),
		   y.BatchPrice,
		   y.InvoiceNumber,
		   ISNULL(numberOfStockEvents.cnt, 0),
		   ISNULL(numberOfSaleEvents.cnt, 0),
		   ISNULL(segments.cnt, 1)
     FROM (
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
				 SUM(b.Price)         BatchPrice,
				 STRING_AGG(b.InvoiceNr, ';')     InvoiceNumber				 
			FROM MaterialBatch     b
	  INNER JOIN @keys             k ON (k.BatchNumber = b.BatchNumber AND k.MaterialId = b.MaterialId) 
	  INNER JOIN Material          m ON (b.MaterialId = m.Id)
	  INNER JOIN MaterialInventory i ON (m.InventoryId = i.Id)  
	  CROSS APPLY ConvertToSmallestUnit(b.Volume, b.UnitId) convertedVolume    
	  INNER JOIN MaterialUnit      u ON (convertedVolume.TargetUnitId = u.Id)
   
  GROUP BY b.CalculatedKey, 
	       i.Name,
		   b.BatchNumber,
		   m.Name,
		   m.Id,
		   u.Symbol) as y

  LEFT JOIN (SELECT b.CalculatedKey, COUNT(DISTINCT component.CalculatedKey) as cnt
               FROM MaterialBatch b
			   JOIN MaterialBatchComposition mbc ON mbc.CompositionId = b.Id
			   JOIN MaterialBatch component ON mbc.ComponentId = component.Id
			 GROUP BY b.CalculatedKey) as numberOfComponents ON (y.BatchId = numberOfComponents.CalculatedKey)	

  LEFT JOIN (SELECT b.CalculatedKey, COUNT(DISTINCT composition.CalculatedKey) as cnt
               FROM MaterialBatch b
			   JOIN MaterialBatchComposition mbc ON mbc.ComponentId = b.Id
			   JOIN MaterialBatch composition ON mbc.CompositionId = composition.Id
			 GROUP BY b.CalculatedKey) as numberOfCompositions ON (y.BatchId = numberOfCompositions.CalculatedKey)
	
  LEFT JOIN (SELECT mb.CalculatedKey, COUNT(DISTINCT po.Id) as cnt
               FROM       PurchaseOrder po
			   INNER JOIN OrderItem     oi ON (oi.PurchaseOrderId = po.Id)
			   LEFT JOIN  OrderItem     ki ON (ki.KitParentId = oi.Id)
			   INNER JOIN OrderItemMaterialBatch oimb ON (oimb.OrderItemId = ISNULL(ki.Id, oi.Id))
			   INNER JOIN MaterialBatch mb ON (oimb.MaterialBatchId = mb.Id)
			 GROUP BY mb.CalculatedKey) as numberOfOrders ON (numberOfOrders.CalculatedKey = y.BatchId)

   LEFT JOIN (SELECT mb.CalculatedKey, COUNT(DISTINCT se.EventGroupingKey) as cnt 
                FROM MaterialStockEvent se
				JOIN MaterialBatch mb ON se.BatchId = mb.Id
			  GROUP BY mb.CalculatedKey) as numberOfStockEvents ON (numberOfStockEvents.CalculatedKey = y.BatchId)

   LEFT JOIN (SELECT mb.CalculatedKey, COUNT(DISTINCT se.Id) as cnt
                FROM SaleEvent se
				JOIN SaleEventAllocation sea ON sea.SaleEventId = se.Id
				JOIN MaterialBatch mb ON sea.BatchId = mb.Id
			  GROUP BY mb.CalculatedKey) as numberOfSaleEvents ON (numberOfSaleEvents.CalculatedKey = y.BatchId)

   LEFT JOIN (SELECT smb.CalculatedKEy, COUNT(DISTINCT smb.Id) as cnt
                FROM MaterialBatch smb
			  GROUP BY smb.CalculatedKey) segments ON (y.BatchId = segments.CalculatedKey)

  ORDER BY y.BatchCreateDt DESC;

END


GO


