UPDATE StockEventType
   SET GenerateForAutofinalization = 1
 WHERE NOT EXISTS(SELECT TOP 1 1 FROM StockEventType WHERE GenerateForAutofinalization = 1)
   AND Name = N'Odpad';

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'autosp_autofinalizeAbandonedBatches')
	DROP PROCEDURE autosp_autofinalizeAbandonedBatches;

GO

CREATE PROCEDURE autosp_autofinalizeAbandonedBatches (@projectId INT)
AS
BEGIN
 
	DECLARE @eventTypeId INT = (SELECT TOP 1 Id FROM StockEventType WHERE GenerateForAutofinalization = 1);
	DECLARE @robotUserId INT = (SELECT TOP 1 Id FROM [User] WHERE ProjectId = @projectId AND EMail like 'robot.%');

	WITH cte
	AS
	(
		SELECT m.Id MaterialId, mb.Id BatchId, bevt.EventDt
		 FROM vwBatchEvent bevt
		 JOIN MaterialBatch mb ON (bevt.BatchId = mb.Id)
		 JOIN Material m ON (mb.MaterialId = m.Id)
		WHERE bevt.EventName = 'USED_AS_COMPONENT'
	)

	INSERT INTO MaterialStockEvent 
	(BatchId, Delta, UnitId,                             ProjectId, TypeId, Note, UserId, EventGroupingKey, EventDt)
	SELECT component.Id, bavam.Available, bavam.UnitId, @projectId, @eventTypeId, N'Automaticky - Odpad z výroby', @robotUserId, LEFT(CAST(NEWID() AS NVARCHAR(255)), 32), GETDATE()
	  FROM MaterialBatch component 
	  JOIN (
		SELECT bam.MaterialId, bam.BatchNumber
		  FROM vwBatchKeyAvailableAmount bam
		  JOIN Material m ON (bam.MaterialId = m.Id)  
		  JOIN (SELECT MaterialId, MAX(EventDt) LastEvent
				  FROM cte
				GROUP BY MaterialId) as LastEvt ON (LastEvt.MaterialId = m.Id)
		  JOIN cte c ON (c.MaterialId = m.Id AND c.EventDt = LastEvt.LastEvent)
		  JOIN MaterialBatch lastUsedBatch ON (lastUsedBatch.Id = c.BatchId)
		WHERE m.ProjectId = @projectId
		  AND m.UseAutofinalization = 1
		  AND bam.Available > 0 
		  AND ((bam.Available / bam.BatchTotal) <= 0.1)
		  AND bam.BatchNumber <> lastUsedBatch.BatchNumber) bkey ON (bkey.BatchNumber = component.BatchNumber AND bkey.MaterialId = component.MaterialId)
	  JOIN vwBatchAvailableAmount bavam ON (bavam.BatchId = component.Id)

END