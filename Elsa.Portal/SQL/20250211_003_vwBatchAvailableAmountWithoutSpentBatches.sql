IF EXISTS(SELECT * FROM sys.objects WHERE name ='vwBatchAvailableAmountWithoutSpentBatches')
BEGIN
	DROP VIEW vwBatchAvailableAmountWithoutSpentBatches;
END

GO

CREATE VIEW vwBatchAvailableAmountWithoutSpentBatches
AS
SELECT be.BatchId, b.ProjectId, b.MaterialId, m.InventoryId, ISNULL(uc._PreferredTargetUnitId, be.DeltaUnitId) UnitId, SUM(be.Delta * ISNULL(uc._MultiplierToPreferredTargetUnit, 1)) Available
	  FROM MaterialBatch b
	  JOIN Material         m  ON (b.MaterialId = m.Id)
	  JOIN vwBatchEvent     be ON (b.Id = be.BatchId)
	  LEFT JOIN vwUnitConversion uc ON (uc.SourceUnitId = be.DeltaUnitId AND uc._PreferredTargetUnitId IS NOT NULL AND uc._PreferredTargetUnitId = uc.TargetUnitId)
	  WHERE b.FullSpendDt IS NULL
GROUP BY be.BatchId, b.ProjectId, b.MaterialId, m.InventoryId, ISNULL(uc._PreferredTargetUnitId, be.DeltaUnitId);
