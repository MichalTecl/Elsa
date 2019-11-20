
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


