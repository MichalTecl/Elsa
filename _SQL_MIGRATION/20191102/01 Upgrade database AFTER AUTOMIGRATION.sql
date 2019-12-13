if exists(select * from sys.views v where v.name = 'vwUnitConversion')
BEGIN
	DROP VIEW vwUnitConversion;
END

GO
CREATE VIEW vwUnitConversion
AS
SELECT uc.ProjectId, uc.SourceUnitId, uc.TargetUnitId, uc.Multiplier  
	FROM UnitConversion uc   
UNION
	SELECT uc.ProjectId, uc.TargetUnitId as SourceUnitId, uc.SourceUnitId as TargetUnitId, 1/uc.Multiplier  
	FROM UnitConversion uc 
UNION
	SELECT mu.ProjectId, mu.Id SourceUnitId, mu.Id TargetUnitId, 1 as Multiplier
  	FROM MaterialUnit mu;
	
GO

ALTER FUNCTION [dbo].[ConvertToUnit] 
(
	@ProjectId INT,
	@Value DECIMAL(19,4),
	@SourceUnitId INT,
	@TargetUnitId INT
)
RETURNS DECIMAL(19,4)
AS
BEGIN
	IF (@SourceUnitId = @TargetUnitId)
	BEGIN
		RETURN @Value;
	END


	RETURN @Value * (SELECT TOP 1 Multiplier FROM vwUnitConversion WHERE ProjectId = @ProjectId AND SourceUnitId = @SourceUnitId AND TargetUnitId = @TargetUnitId);
END

GO

IF EXISTS (SELECT *
           FROM   sys.objects
           WHERE  object_id = OBJECT_ID(N'[dbo].[ConvertToSmallestUnit]')
                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
BEGIN
	DROP FUNCTION ConvertToSmallestUnit;
END

GO

CREATE FUNCTION ConvertToSmallestUnit 
(
	@sourceValue DECIMAL(19,5),
	@sourceUnitId INT	 	
)
RETURNS TABLE
AS
RETURN
	SELECT @sourceValue * uc.Multiplier Value, uc.TargetUnitId
	  FROM MaterialUnit sourceUnit
	  JOIN (
			SELECT co.sourceUnitId, MAX(co.Multiplier) Multiplier
			  FROM vwUnitConversion co		
			GROUP BY co.ProjectId, co.sourceUnitId 
	  ) bestMatch ON (bestMatch.SourceUnitId = sourceUnit.Id)
	  JOIN vwUnitConversion uc ON (uc.SourceUnitId = bestMatch.SourceUnitId AND uc.Multiplier = bestMatch.Multiplier)
	WHERE sourceUnit.Id = @sourceUnitId;

GO

IF EXISTS (SELECT *
           FROM   sys.objects
           WHERE  object_id = OBJECT_ID(N'[dbo].[ParseBatchKey]')
                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
BEGIN
	DROP FUNCTION ParseBatchKey;
END

GO

CREATE FUNCTION ParseBatchKey 
(	
	@batchKey nvarchar(100)
)
RETURNS TABLE 
AS
RETURN
	SELECT 
	CASE WHEN CHARINDEX(':', @batchKey) > 1 
		THEN
			SUBSTRING(@batchKey, 1, CHARINDEX(':', @batchKey) - 1) 
		ELSE
			NULL
		END
			as BatchNumber,
	CASE WHEN CHARINDEX(':', @batchKey) > 1 
		THEN
			CONVERT(INT, SUBSTRING(@batchKey, CHARINDEX(':', @batchKey) + 1, 9999)) 
		ELSE
			NULL
		END as MaterialId;

GO


ALTER PROCEDURE [dbo].[DeleteInvoiceFormCollection](@collectionId INT)
AS
BEGIN
	DECLARE @formItems TABLE (id INT);

	INSERT INTO @formItems
	    SELECT itm.Id
	      FROM InvoiceFormCollection coll
    INNER JOIN InvoiceForm           frm  ON (coll.Id = frm.InvoiceFormCollectionId) 
	INNER JOIN InvoiceFormItem       itm  ON (itm.InvoiceFormId = frm.Id)
	     WHERE coll.Id = @collectionId;

    DELETE FROM InvoiceFormGenerationLog WHERE InvoiceFormCollectionId = @collectionId;

	DELETE FROM MaterialBatchCompositionFormItem WHERE InvoiceFormItemId IN (SELECT Id FROM @formItems);	
	DELETE FROM OrderItemInvoiceFormItem         WHERE InvoiceFormItemId IN (SELECT Id FROM @formItems);
	DELETE FROM InvoiceFormItemMaterialBatch     WHERE InvoiceFormItemId IN (SELECT Id FROM @formItems);
	DELETE FROM StockEventInvoiceFormItem        WHERE InvoiceFormItemId IN (SELECT Id FROM @formItems);	
	DELETE FROM InvoiceFormItem WHERE Id IN (SELECT Id FROM @formItems);
	DELETE FROM InvoiceForm     WHERE InvoiceFormCollectionId = @collectionId;
	DELETE FROM InvoiceFormCollection WHERE Id = @collectionId;
END

GO
	

ALTER PROCEDURE [dbo].[CalculateBatchUsages] (@ProjectId INT, @BatchId INT = NULL, @MaterialId INT = NULL, @debug BIT = NULL)
AS
BEGIN	
	DECLARE @batches TABLE (Id INT, CalcUnitId INT);
	DECLARE @stat TABLE (BatchId INT NOT NULL, OperationType INT NOT NULL, Amount DECIMAL(19, 4) NOT NULL);

	DECLARE @T_INITIAL_VOLUME INT = 1;
	DECLARE @T_LIMIT INT = 2;
	DECLARE @T_SUBTRACT INT = 3;

	INSERT INTO @batches (Id, CalcUnitId)
	SELECT b.Id, ISNULL(conversionToTargetUnit.TargetUnitId, m.NominalUnitId)
	  FROM MaterialBatch b
     INNER JOIN Material m ON (m.Id = b.MaterialId)
	 LEFT  JOIN (
		SELECT uc.SourceUnitId, MAX(uc.Multiplier) mul
		  FROM UnitConversion uc
		GROUP BY uc.SourceUnitId
	 ) highMultiplier ON (highMultiplier.SourceUnitId = m.NominalUnitId)
	 LEFT JOIN UnitConversion conversionToTargetUnit ON (conversionToTargetUnit.SourceUnitId = highMultiplier.SourceUnitId AND conversionToTargetUnit.Multiplier = highMultiplier.mul)
	 WHERE b.CloseDt IS NULL
	   AND b.IsAvailable = 1
	   AND b.ProjectId = @ProjectId
	   AND ((@BatchId IS NULL) OR (@BatchId = b.Id))
	   AND ((@MaterialId IS NULL) OR (b.MaterialId = @MaterialId));

	IF NOT EXISTS(SELECT TOP 1 1 FROM @batches)
	BEGIN
		RETURN;
	END

	-- Initial volume
	INSERT INTO @stat (BatchId, OperationType, Amount)
	SELECT b.Id, @T_INITIAL_VOLUME, dbo.ConvertToUnit(@ProjectId,  b.Volume, b.UnitId,  src.CalcUnitId)
	  FROM MaterialBatch b	 
	INNER JOIN @batches src ON (b.Id = src.Id);

	-- Stock events	
	INSERT INTO @stat (BatchId, OperationType, Amount)
	SELECT evt.BatchId, @T_SUBTRACT, dbo.ConvertToUnit(@ProjectId, evt.Delta, evt.UnitId, src.CalcUnitId)
	  FROM MaterialStockEvent evt
	  INNER JOIN @batches src ON (src.Id = evt.BatchId);

	-- Used as components
	INSERT INTO @stat (BatchId, OperationType, Amount)
	SELECT mbc.ComponentId, @T_SUBTRACT, dbo.ConvertToUnit(@ProjectId, mbc.Volume, mbc.UnitId, src.CalcUnitId)
	  FROM MaterialBatchComposition mbc
	  INNER JOIN @batches src ON (src.Id = mbc.ComponentId);

	-- Used in orders
	INSERT INTO @stat (BatchId, OperationType, Amount)
	SELECT oimb.MaterialBatchId, @T_SUBTRACT, dbo.ConvertToUnit(@ProjectId, oimb.Quantity, m.NominalUnitId, src.CalcUnitId)
	      FROM PurchaseOrder po
	INNER JOIN OrderItem               lineItem  ON (lineItem.PurchaseOrderId = po.Id)
	 LEFT JOIN OrderItem               kitItem   ON (kitItem.KitParentId = lineItem.Id)
    INNER JOIN OrderItemMaterialBatch  oimb      ON (ISNULL(kitItem.Id, lineItem.Id) = oimb.OrderItemId)
	INNER JOIN @batches                src       ON (src.Id = oimb.MaterialBatchId)
	INNER JOIN MaterialBatch           mb        ON (mb.Id = oimb.MaterialBatchId)    
	INNER JOIN Material                m         ON (m.Id = mb.MaterialId)
	WHERE po.OrderStatusId NOT IN (/*6,*/ 7, 8); -- 6 = returned => it shouldn't be automatically returned back to stock

	-- Sale events
	INSERT INTO @stat (BatchId, OperationType, Amount)
	SELECT sea.BatchId, @T_SUBTRACT, sea.AllocatedQuantity - ISNULL(sea.ReturnedQuantity, 0)
	  FROM SaleEventAllocation sea
     WHERE sea.BatchId IN (SELECT Id FROM @batches);

	IF (ISNULL(@debug, 0) = 1)
	BEGIN
		select * from @stat WHERE BatchId = @BatchId;
		
		SELECT src.Id as BatchId, mb.BatchNumber,
		 /*src.Id as BatchId, src.CalcUnitId as UnitId, */
		 ISNULL(Limit.Amount, Initial.Amount) - ISNULL(Subtract.Amount, 0)
		 , u.Symbol

		  FROM @batches src
		join MaterialBatch mb ON (mb.ID = src.Id)
		join MaterialUnit  u  ON (u.Id = src.CalcUnitId)
		LEFT JOIN (SELECT s1.BatchId, SUM(s1.Amount) Amount FROM @stat s1 WHERE s1.OperationType = 1 GROUP BY s1.BatchId) 
			as Initial ON (src.Id = Initial.BatchId)

		LEFT JOIN (SELECT s2.BatchId, MIN(s2.Amount) Amount FROM @stat s2 WHERE s2.OperationType = 2 GROUP BY s2.BatchId) 
			as Limit ON (src.Id = Limit.BatchId)

		LEFT JOIN (SELECT s3.BatchId, SUM(s3.Amount) Amount FROM @stat s3 WHERE s3.OperationType = 3 GROUP BY s3.BatchId) 
			as Subtract ON (src.Id = Subtract.BatchId)

		ORDER by src.Id;
	END
	ELSE
	BEGIN
		SELECT src.Id as BatchId, src.CalcUnitId as UnitId, ISNULL(Limit.Amount, Initial.Amount) - ISNULL(Subtract.Amount, 0) as Available
		  FROM @batches src
		join MaterialBatch mb ON (mb.ID = src.Id)
		join MaterialUnit  u  ON (u.Id = src.CalcUnitId)
		LEFT JOIN (SELECT s1.BatchId, SUM(s1.Amount) Amount FROM @stat s1 WHERE s1.OperationType = 1 GROUP BY s1.BatchId) 
			as Initial ON (src.Id = Initial.BatchId)

		LEFT JOIN (SELECT s2.BatchId, MIN(s2.Amount) Amount FROM @stat s2 WHERE s2.OperationType = 2 GROUP BY s2.BatchId) 
			as Limit ON (src.Id = Limit.BatchId)

		LEFT JOIN (SELECT s3.BatchId, SUM(s3.Amount) Amount FROM @stat s3 WHERE s3.OperationType = 3 GROUP BY s3.BatchId) 
			as Subtract ON (src.Id = Subtract.BatchId);
	END
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures p where p.name = 'GetMaterialResolution')
BEGIN
	DROP PROCEDURE GetMaterialResolution;
END 

GO

CREATE PROCEDURE GetMaterialResolution (
	@projectId INT,
	@materialId INT)
AS
BEGIN
	DECLARE @avail TABLE (BatchId INT, UnitId INT, Available DECIMAL(19,5));
	INSERT INTO @avail 
	exec CalculateBatchUsages 1, null, @materialId;

	SELECT b.Id BatchId, b.BatchNumber, avail.Available, avail.UnitId, b.Created 
	  FROM @avail avail
	  JOIN MaterialBatch b ON (b.Id = avail.BatchId)
	 WHERE avail.Available > 0
	   AND b.CloseDt IS NULL
	ORDER BY b.Created;
END
  
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
		   ISNULL(numberOfSaleEvents.cnt, 0)
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

  ORDER BY y.BatchProductionDt DESC;

END


GO

ALTER PROCEDURE [dbo].[sp_backup]
AS
BEGIN

	DECLARE @fileName VARCHAR(100) = 'c:\DBBAK\Elsa_' + SUBSTRING(@@SERVERNAME, 0, CHARINDEX('\', @@SERVERNAME)) + '_' + convert(nvarchar(MAX), GETDATE(), 12) + '.bak';
	PRINT @fileName;
	
	DECLARE @dbName VARCHAR(100);
	SET @dbName = (SELECT DB_NAME());
		
	BACKUP DATABASE @dbName TO DISK = @fileName;

	SELECT @fileName;
END
GO



	