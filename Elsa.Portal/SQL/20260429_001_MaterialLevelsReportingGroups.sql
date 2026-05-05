IF OBJECT_ID(N'[dbo].[UserHiddenMaterialLevelTab]', N'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[UserHiddenMaterialLevelTab](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[InventoryId] [int] NOT NULL,
		[MaterialLevelReportingGroup] [nvarchar](256) NULL,
		[UserId] [int] NOT NULL,
		CONSTRAINT [PK_UserHiddenMaterialLevelTab] PRIMARY KEY CLUSTERED ([Id] ASC)
	);
END

GO

CREATE OR ALTER PROCEDURE [dbo].[GetMaterialLevelsReport](
	 @inventoryId INT,
	 @projectId INT,
	 @materialLevelReportingGroup NVARCHAR(256) = NULL)
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
		   orderEvent.UserId OrderEventUserId,
		   orderEvent.DeliveryDeadline DeliveryDeadline
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
		  AND mb.CloseDt IS NULL
		  AND (
				(@materialLevelReportingGroup IS NULL AND NULLIF(LTRIM(RTRIM(m.MaterialLevelReportingGroup)), '') IS NULL)
				OR NULLIF(LTRIM(RTRIM(m.MaterialLevelReportingGroup)), '') = @materialLevelReportingGroup
			  )) x
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

CREATE OR ALTER PROCEDURE GetThresholdsState(@projectId INT, @userId INT)
AS
BEGIN

	SELECT bam.InventoryId,
	       NULLIF(LTRIM(RTRIM(m.MaterialLevelReportingGroup)), '') MaterialLevelReportingGroup,
	       bam.MaterialId,
		   m.Name MaterialName,
		   th.ThresholdQuantity,
		   th.UnitId,
		   SUM(uc.Multiplier * bam.Available) Available
	  FROM vwBatchAvailableAmount bam
	  JOIN Material m ON (bam.MaterialId = m.Id)
	  JOIN MaterialThreshold th ON (th.MaterialId = m.Id)
	  LEFT JOIN vwUnitConversion uc ON (uc.SourceUnitId = bam.UnitId AND uc.TargetUnitId = th.UnitId)
	  LEFT JOIN UserHiddenMaterialLevelTab hiddenTab ON (
			hiddenTab.UserId = @userId
			AND hiddenTab.InventoryId = m.InventoryId
			AND ISNULL(NULLIF(LTRIM(RTRIM(hiddenTab.MaterialLevelReportingGroup)), ''), '') = ISNULL(NULLIF(LTRIM(RTRIM(m.MaterialLevelReportingGroup)), ''), '')
		)
	WHERE hiddenTab.Id IS NULL
	  AND bam.ProjectId = @projectId	  
	GROUP BY bam.InventoryId,
	       NULLIF(LTRIM(RTRIM(m.MaterialLevelReportingGroup)), ''),
	       bam.MaterialId,
		   m.Name,
		   th.ThresholdQuantity,
		   th.UnitId
    HAVING  SUM(uc.Multiplier * bam.Available) < th.ThresholdQuantity
END
