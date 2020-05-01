IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'vwMaterialSupplier')
BEGIN
	DROP VIEW vwMaterialSupplier;
END

GO

CREATE VIEW vwMaterialSupplier 
AS
SELECT DISTINCT mb.MaterialId, mb.SupplierId 
  FROM MaterialBatch mb
  JOIN (SELECT x.MaterialId materialId, MAX(x.Created) latest
          FROM MaterialBatch x
		  JOIN  Supplier sup ON (x.SupplierId = sup.Id)
		 WHERE (sup.DisableDt IS NULL) OR (sup.DisableDt > GETDATE())
		GROUP BY x.MaterialId) newsetBatch ON (mb.MaterialId = newsetBatch.materialId AND mb.Created = newsetBatch.latest);

GO

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
	SELECT y.MaterialId, y.MaterialName, y.BatchNumber, y.UnitId, y.Available, sup.Name SupplierName, sup.ContactEmail SupplierEmail, sup.ContactPhone SupplierPhone
	FROM 
	(
		SELECT x.MaterialId, x.MaterialName, x.BatchNumber, x.UnitId, SUM(x.Available) Available
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
		GROUP BY x.MaterialId, x.MaterialName, x.BatchNumber, x.UnitId) y
	  LEFT JOIN vwMaterialSupplier msup ON (y.MaterialId = msup.MaterialId)
	  LEFT JOIN Supplier sup ON (msup.SupplierId = sup.Id)
	ORDER BY y.MaterialName, y.BatchNumber;
END

GO

UPDATE Supplier 
   SET DisableDt = GETDATE(),
       DisableUserId = 2
WHERE DisableDt IS NULL 
  AND Name = N'Aneta Adamčíková';