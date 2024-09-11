IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'LoadBatchesReport_Fast')
	DROP PROCEDURE LoadBatchesReport_Fast;

GO

CREATE PROCEDURE [LoadBatchesReport_Fast] (
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
	@segmentId INT = NULL,
	@invoiceNr NVARCHAR(100) = NULL
	)
AS
BEGIN
	
	IF (@orderNumber IS NOT NULL)
	BEGIN	    
		SELECT TOP 1 @orderId = Id 
		  FROM PurchaseOrder po
		 WHERE po.OrderNumber = @orderNumber;
	END;
				
	WITH keys
	AS	
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
		  AND ((@invoiceNr IS NULL) OR (b.InvoiceNr = @invoiceNr))
		  AND ((@orderId IS NULL) OR (b.Id IN 
												(SELECT woib.MaterialBatchId
												   FROM vwOrderItems woi												   
												   JOIN OrderItemMaterialBatch woib ON (woi.OrderItemId = woib.OrderItemId)
												  WHERE woi.OrderId = @orderId)))

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
		)

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
		   0 NumberOfComponents,
		   0 NumberOfCompositions,
		   0 as numberOfMaterialSteps, 		   		   
		   0 NumberOfOrders,
		   y.BatchPrice,
		   y.InvoiceNumber,
		   0 NumberOfStockEvents,
		   0 NumberOfSaleEvents,
		   0 NumberOfSegments,
		   ISNULL(availam.Available, 0) AvailableAmountVal,
		   availam.UnitId    AvailableAmountUnitId
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
	  INNER JOIN keys              k ON (k.BatchNumber = b.BatchNumber AND k.MaterialId = b.MaterialId) 
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
		     
    LEFT JOIN vwBatchKeyAvailableAmount availam ON (availam.BatchNumber = y.BatchNumber AND availam.MaterialId = y.MaterialId)

  ORDER BY y.BatchCreateDt DESC;

END
GO


