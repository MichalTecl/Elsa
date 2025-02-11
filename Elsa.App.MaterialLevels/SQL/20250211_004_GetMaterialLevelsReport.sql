IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'GetMaterialLevelsReport')
BEGIN
	DROP PROCEDURE GetMaterialLevelsReport;
END

GO

CREATE PROCEDURE [dbo].[GetMaterialLevelsReport](
	 @inventoryId INT,
	 @projectId INT)
AS
BEGIN
	SELECT y.MaterialId, 
	       y.MaterialName, 
		   y.BatchNumber, 
		   y.UnitId, 
		   y.Available, 
		   sup.Name SupplierName, 
		   sup.ContactEmail SupplierEmail, 
		   sup.ContactPhone SupplierPhone,
		   orderEvent.OrderDt OrderDt,
		   orderEvent.UserId OrderEventUserId
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
		  LEFT JOIN vwBatchAvailableAmountWithoutSpentBatches bam ON (bam.MaterialId = m.Id AND bam.Available > 0)
		  LEFT JOIN MaterialBatch mb ON (bam.BatchId = mb.Id)
		 
		WHERE m.InventoryId = @inventoryId
		  AND mb.CloseDt IS NULL) x
		GROUP BY x.MaterialId, x.MaterialName, x.BatchNumber, x.UnitId) y
	  
	  LEFT JOIN vwMaterialSupplier msup ON (y.MaterialId = msup.MaterialId)
	  LEFT JOIN Supplier sup ON (msup.SupplierId = sup.Id)
	  LEFT JOIN (SELECT oe.MaterialId, MAX(oe.OrderDt) Dt
		               FROM MaterialOrderEvent oe
					  WHERE NOT EXISTS(SELECT TOP 1 1 
					                     FROM MaterialBatch b
										WHERE b.MaterialId = oe.MaterialId
										  AND b.Created > oe.OrderDt)
					 GROUP BY oe.MaterialId) lastOrderEvent ON (lastOrderEvent.MaterialId = y.MaterialId)
		LEFT JOIN MaterialOrderEvent orderEvent ON (orderEvent.MaterialId = lastOrderEvent.MaterialId AND orderEvent.OrderDt = lastOrderEvent.Dt)
	ORDER BY y.MaterialName, y.BatchNumber;
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'SaveMaterialOrderDt')
	DROP PROCEDURE SaveMaterialOrderDt;

GO

CREATE PROCEDURE SaveMaterialOrderDt(@materialId INT, @orderDt DATETIME, @userId INT)
AS
BEGIN
	
	DELETE FROM MaterialOrderEvent
	 WHERE ID IN (
		SELECT oe.Id
		  FROM MaterialOrderEvent oe	  
		 WHERE oe.MaterialId = @materialId
		   AND NOT EXISTS(SELECT TOP 1 1
							FROM MaterialBatch mb
						   WHERE mb.MaterialId = @materialId
							 AND mb.Created > oe.OrderDt));
	
	IF (@orderDt IS NOT NULL)
	BEGIN
		INSERT INTO MaterialOrderEvent (MaterialId, OrderDt, InsertDt, UserId)
		VALUES (@materialId, @orderDt, GETDATE(), @userId);
	END

END