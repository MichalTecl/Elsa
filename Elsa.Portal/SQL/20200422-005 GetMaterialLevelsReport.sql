IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'GetMaterialLevelsReport')
BEGIN
	DROP PROCEDURE GetMaterialLevelsReport;
END

GO

CREATE PROCEDURE GetMaterialLevelsReport(
	 @inventoryId INT,
	 @projectId INT)
AS
BEGIN
	SELECT x.MaterialId, x.MaterialName, x.BatchNumber, x.UnitId, SUM(x.Available)
	  FROM (
	SELECT m.Id MaterialId,
	       m.Name MaterialName,
		   mb.BatchNumber BatchNumber,
		   ISNULL(bam.UnitId, m.NominalUnitId) UnitId,
		   ISNULL(bam.Available, 0) Available
	  FROM Material m
	  LEFT JOIN vwBatchAvailableAmount bam ON (bam.MaterialId = m.Id AND bam.Available > 0)
	  LEFT JOIN MaterialBatch mb ON (bam.BatchId = mb.Id)
	WHERE m.InventoryId = @inventoryId
	  AND mb.CloseDt IS NULL) x
    GROUP BY x.MaterialId, x.MaterialName, x.BatchNumber, x.UnitId
	ORDER BY x.MaterialName, x.BatchNumber;
END