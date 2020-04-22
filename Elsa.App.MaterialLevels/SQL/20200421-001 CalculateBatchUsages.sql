GO

ALTER PROCEDURE CalculateBatchUsages (@ProjectId INT, @BatchId INT = NULL, @MaterialId INT = NULL, @repDate DATETIME = NULL, @debug BIT = NULL, @inventoryId INT = NULL)
AS
BEGIN	
	IF (@repDate IS NULL)
	BEGIN
		SET @repdate = GETDATE() + 365;
	END

	SELECT be.BatchId, ISNULL(uc._PreferredTargetUnitId, be.DeltaUnitId) UnitId, SUM(be.Delta * ISNULL(uc._MultiplierToPreferredTargetUnit, 1)) Available
	  FROM MaterialBatch b
	  JOIN Material         m  ON (b.MaterialId = m.Id)
	  JOIN vwBatchEvent     be ON (b.Id = be.BatchId)
	  LEFT JOIN vwUnitConversion uc ON (uc.SourceUnitId = be.DeltaUnitId AND uc._PreferredTargetUnitId IS NOT NULL AND uc._PreferredTargetUnitId = uc.TargetUnitId)
	WHERE	
	 ((@BatchId IS NOT NULL) OR (b.CloseDt IS NULL) OR (b.CloseDt > @repDate))
	   AND b.IsAvailable = 1
	   AND b.ProjectId = @ProjectId
	   AND ((@BatchId IS NULL) OR (@BatchId = b.Id))
	   AND ((@MaterialId IS NULL) OR (b.MaterialId = @MaterialId))
	   AND b.Created <= @repDate
	   AND ((@inventoryId IS NULL) OR m.InventoryId = @inventoryId)
       AND (be.EventDt <= @repDate)
	 GROUP BY  be.BatchId, ISNULL(uc._PreferredTargetUnitId, be.DeltaUnitId);
END