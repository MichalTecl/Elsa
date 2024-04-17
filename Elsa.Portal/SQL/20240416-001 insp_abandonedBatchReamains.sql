IF EXISTS(SELECT TOP 1 1 FROM sys.views WHERE name = 'vwMaterialMostRecentBatch')
	DROP VIEW vwMaterialMostRecentBatch;

GO

CREATE VIEW vwMaterialMostRecentBatch
AS
WITH materialEvent
  AS (SELECT mb.Id BatchId, mb.MaterialId, be.EventDt dt
	  FROM vwBatchEvent be
	  JOIN MaterialBatch mb ON (be.BatchId = mb.Id)
	 WHERE be.EventName IN ('STOCK_EVENT', 'DIRECT_SALE_ALLOCATION', 'DIRECT_SALE_RETURN', 'USED_AS_COMPONENT', 'ESHOP_SALE')
	 )
SELECT lastEvent.MaterialId, lastEvent.BatchId
  FROM (SELECT me.MaterialId, MAX(me.dt) lastEvent FROM materialEvent me GROUP BY me.MaterialId) lastMaterialManipulation
  JOIN materialEvent lastEvent ON (lastMaterialManipulation.MaterialId = lastEvent.MaterialId AND lastMaterialManipulation.lastEvent = lastEvent.dt)

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.views WHERE name = 'vwAbandonedBatches')
	DROP VIEW vwAbandonedBatches;

GO


CREATE VIEW vwAbandonedBatches
AS
SELECT x.*, 
       DATEDIFF(day, x.EventDt, GETDATE()) DaysFromEvent,
	   CASE 
	       WHEN 
			(DATEDIFF(day, x.EventDt, GETDATE()) >= x.DaysBeforeWarnForUnused) AND ((x.NotAbandonedUntilNewerBatchUsed = 0) OR (x.MostRecentBatch > x.BatchId)) 
		   THEN 1 
		   ELSE 0 
	   END IsAbandoned
  FROM (
	SELECT m.ProjectId, 
		   mb.Id BatchId, 
		   m.Id MaterialId, 
		   mb.BatchNumber, 
		   m.Name MaterialName, 
		   m.DaysBeforeWarnForUnused, 
		   ISNULL(m.UseAutofinalization, 0) Autofinalize, 
		   ISNULL(m.UsageProlongsLifetime, 0) UsageProlongsLifetime, 
		   ISNULL(m.NotAbandonedUntilNewerBatchUsed, 0) NotAbandonedUntilNewerBatchUsed,
		   ISNULL(le.LastEvent, mb.Created) EventDt,
		   ISNULL(mrb.BatchId, mb.Id) MostRecentBatch
	  FROM Material m
	  JOIN MaterialBatch mb ON (m.Id = mb.MaterialId)
	  JOIN vwBatchAvailableAmount bam ON (mb.Id = bam.BatchId)
	  LEFT JOIN (SELECT be.BatchId, MAX(be.EventDt) LastEvent
				   FROM vwBatchEvent be 
				  WHERE be.EventName IN ('DIRECT_SALE_ALLOCATION', 'DIRECT_SALE_RETURN', 'USED_AS_COMPONENT', 'ESHOP_SALE')
				 GROUP BY be.BatchId) le ON (m.UsageProlongsLifetime = 1 AND le.BatchId = mb.Id)
	  LEFT JOIN vwMaterialMostRecentBatch mrb ON (mrb.MaterialId = m.Id)
	 WHERE m.DaysBeforeWarnForUnused IS NOT NULL
	   AND mb.CloseDt IS NULL
	   AND bam.Available > 0) x
GO



IF EXISTS(SELECT TOP 1 1 
            FROM sys.procedures
		   WHERE name = 'insp_abandonedBatchRemains')
BEGIN
	DROP PROCEDURE insp_abandonedBatchRemains;
END

GO

CREATE PROCEDURE insp_abandonedBatchRemains (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	DECLARE @retryBatchNr NVARCHAR(200) = (SELECT TOP 1 ida.StrValue
	                               FROM InspectionIssueData ida 
								  WHERE ida.IssueId = @retryIssueId
								    AND ida.PropertyName = 'BatchNumber');
	DECLARE @retryMaterialId INT = (SELECT TOP 1 ida.IntValue
	                                  FROM InspectionIssueData ida
									 WHERE ida.IssueId = @retryIssueId
									   AND ida.PropertyName = 'MaterialId');  

	 
	DECLARE @isu TABLE (inspType NVARCHAR(200), 
                code NVARCHAR(200), 
				message NVARCHAR(1000),
				materialId INT,
				amount DECIMAL,
				amountUnitId INT,
				batchNr NVARCHAR(200),
				amtUnitSymbol NVARCHAR(20));
	

	INSERT INTO @isu
	SELECT DISTINCT 
	       N'Opuštěné ' + m.UnusedWarnMaterialType,  --inspType
		   'abandonedBatch2_' + c.BatchNumber + '_' + LTRIM(STR(c.MaterialId)), --code
		   N'Šarže ' + c.BatchNumber + '  - "' + c.MaterialName + N'" nebyla použita ' + LTRIM(STR(c.DaysFromEvent)) + N' dnů' + 
		   CASE WHEN c.NotAbandonedUntilNewerBatchUsed = 1 THEN ' a již byla použita novější šarže.' ELSE '.' END
		   , --message
		 c.MaterialId, -- materialId
		 bam.Available, -- amount
		 bam.UnitId, -- amountUnitId
		 c.BatchNumber, -- batchNr
		 u.Symbol -- amtUnitSymbol
	    FROM vwAbandonedBatches c
	    JOIN Material m ON (c.MaterialId = m.Id)
		JOIN vwBatchAvailableAmount bam ON (c.BatchId = bam.BatchId)
		JOIN MaterialUnit u ON (bam.UnitId = u.Id)
	  WHERE c.ProjectId = @projectId
	    AND m.UnusedWarnMaterialType IS NOT NULL
		AND c.IsAbandoned = 1;

	 DECLARE @stockEventTypeId INT;
	 DECLARE @stockEventTypeName NVARCHAR(200);
	 SELECT TOP 1 @stockEventTypeId = Id, @stockEventTypeName = Name FROM StockEventType WHERE RequiresNote = 1 ANd ProjectId = @projectId;
	 		 
     WHILE(EXISTS(SELECT TOP 1 1 FROM @isu))
	 BEGIN
	    DECLARE @inspType NVARCHAR(200);                    
		DECLARE @materialId INT;
		DECLARE	@amount DECIMAL;
		DECLARE @amountUnitId INT;
		DECLARE @code NVARCHAR(100);
		DECLARE @message NVARCHAR(2000);
		DECLARE @batchNr NVARCHAR(200);
		DECLARE @unitSymbol NVARCHAR(20);

		SELECT TOP 1 @code = code,
					 @inspType = inspType,
					 @materialId = materialId,
					 @amount = amount,
					 @amountUnitId = amountUnitId,
					 @message = message,
					 @batchNr = batchNr,
					 @unitSymbol = amtUnitSymbol  
		  FROM @isu;
  		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, @inspType, @code, @message;

		EXEC inspfw_setIssueDataString @issueId, 'BatchNumber', @batchNr;
		EXEC inspfw_setIssueDataString @issueId, 'Amount', @amount;
		EXEC inspfw_setIssueDataInt @issueId, 'AmountUnitId', @amountUnitId;
		EXEC inspfw_setIssueDataInt @issueId, 'MaterialId', @materialId;
		EXEC inspfw_setIssueDataString @issueId, 'AmtUnitSymbol', @unitSymbol;
		
		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneWeek.html', N'Odložit o týden';
		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneMonth.html', N'Odložit o měsíc';

		IF (@stockEventTypeId IS NOT NULL)
		BEGIN
			DECLARE @stevtText NVARCHAR(1000) = N'Odepsat ' + LTRIM(STR(@amount)) + ' ' + @unitSymbol + N' šarže ' + @batchNr + ' jako ' + @stockEventTypeName;

			EXEC inspfw_setIssueDataString @issueId, 'StockEventText', @stevtText;
			EXEC inspfw_setIssueDataInt @issueId, 'PrefStEventTypeId', @stockEventTypeId;
			EXEC inspfw_setIssueDataString @issueId, 'PrefStEventTypeName', @stockEventTypeName;
			EXEC inspfw_setIssueAction @issueId, '/UI/Controls/Inventory/WarehouseControls/WhActions/StockEventThrashActionButton.html', @stockEventTypeName;
			
		END

		DELETE FROM @isu WHERE Code = @code;
	 END
END
GO
