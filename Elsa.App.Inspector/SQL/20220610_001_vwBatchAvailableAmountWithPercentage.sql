IF EXISTS(SELECT TOP 1 1 FROM sys.views WHERE name = 'vwBatchAvailableAmountWithPercentage')
	DROP VIEW vwBatchAvailableAmountWithPercentage;
GO

CREATE VIEW vwBatchAvailableAmountWithPercentage
AS
SELECT bam.*, 
       mb.Volume * ISNULL(uc.Multiplier, 1) [TotalVolume],
	   (bam.Available / (mb.Volume * ISNULL(uc.Multiplier, 1)) * 100) AvailablePercent
  FROM vwBatchAvailableAmount bam
  JOIN MaterialBatch mb ON (bam.BatchId = mb.Id)
  LEFT JOIN vwUnitConversion uc ON (uc.SourceUnitId = mb.UnitId AND uc.TargetUnitId = bam.UnitId);