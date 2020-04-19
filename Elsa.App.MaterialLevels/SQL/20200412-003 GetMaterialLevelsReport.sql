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
	DECLARE @amounts TABLE (BatchId INT, UnitId INT, Available DECIMAL(19, 4));
	INSERT INTO @amounts
	EXEC CalculateBatchUsages @ProjectId = @projectId, @inventoryId = @inventoryId;

	WITH cte AS (
	SELECT m.Id MaterialId, m.Name MaterialName, mb.BatchNumber, ISNULL(am.UnitId, m.NominalUnitId) as UnitId, SUM(ISNULL(am.Available, 0)) as Available
	  FROM Material m  
	  LEFT JOIN MaterialBatch mb ON (mb.MaterialId = m.Id)
	  LEFT JOIN @amounts am ON (mb.Id = am.BatchId AND am.Available > 0)
	WHERE m.InventoryId = @inventoryId  
	  AND mb.CloseDt IS NULL
	GROUP BY  m.Id, m.Name, mb.BatchNumber, ISNULL(am.UnitId, m.NominalUnitId))
	SELECT * 
	 FROM cte
	WHERE cte.Available > 0 OR NOT EXISTS(SELECT TOP 1 1 FROM cte as nn WHERE nn.MaterialId = cte.MaterialId AND nn.Available > 0)
	ORDER BY cte.MaterialName, cte.BatchNumber;
END

