IF EXISTS(SELECT * FROM sys.objects WHERE name = 'vwBatchKeyAvailableAmount')
BEGIN
	DROP VIEW vwBatchKeyAvailableAmount;
END

GO

CREATE VIEW vwBatchKeyAvailableAmount
AS
SELECT mb.BatchNumber, bam.MaterialId, bam.UnitId, SUM(bam.Available) Available, SUM(mb.Volume * su.Multiplier) BatchTotal
  FROM vwBatchAvailableAmount bam
  JOIN MaterialBatch mb ON (bam.BatchId = mb.Id)
  JOIN vwSmallestUnit su ON (mb.UnitId = su.SourceUnitId)
GROUP BY  mb.BatchNumber, bam.MaterialId, bam.UnitId;

GO