IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERe name = 'vwBatchKeyAvailableAmount')
	DROP VIEW vwBatchKeyAvailableAmount;

GO

CREATE VIEW vwBatchKeyAvailableAmount
AS
SELECT mb.BatchNumber, bam.MaterialId, bam.UnitId, SUM(bam.Available) Available
  FROM vwBatchAvailableAmount bam
  JOIN MaterialBatch mb ON (bam.BatchId = mb.Id)
GROUP BY  mb.BatchNumber, bam.MaterialId, bam.UnitId;

