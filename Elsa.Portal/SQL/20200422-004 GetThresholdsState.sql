IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'GetThresholdsState')
BEGIN
	DROP PROCEDURE GetThresholdsState;
END

GO

CREATE PROCEDURE GetThresholdsState(@projectId INT, @userId INT)
AS
BEGIN

	SELECT bam.InventoryId, 
	       bam.MaterialId,
		   m.Name MaterialName,
		   th.ThresholdQuantity,
		   th.UnitId,
		   SUM(uc.Multiplier * bam.Available) Available
	  FROM vwBatchAvailableAmount bam
	  JOIN Material m ON (bam.MaterialId = m.Id)
	  JOIN MaterialThreshold th ON (th.MaterialId = m.Id)
	  JOIN UserWatchedInventory uwi ON (uwi.InventoryId = m.InventoryId)
	  LEFT JOIN vwUnitConversion uc ON (uc.SourceUnitId = bam.UnitId AND uc.TargetUnitId = th.UnitId)
	WHERE uwi.UserId = @userId
	  AND bam.ProjectId = @projectId	  
	GROUP BY bam.InventoryId, 
	       bam.MaterialId,
		   m.Name,
		   th.ThresholdQuantity,
		   th.UnitId
    HAVING  SUM(uc.Multiplier * bam.Available) < th.ThresholdQuantity
END

