IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'autosp_autofinalizeAbandonedBatches')
	DROP PROCEDURE autosp_autofinalizeAbandonedBatches;

GO

CREATE PROCEDURE autosp_autofinalizeAbandonedBatches (@projectId INT)
AS
BEGIN
 
	DECLARE @eventTypeId INT = (SELECT TOP 1 Id FROM StockEventType WHERE GenerateForAutofinalization = 1);
	DECLARE @robotUserId INT = (SELECT TOP 1 Id FROM [User] WHERE ProjectId = @projectId AND EMail like 'robot.%');

	INSERT INTO MaterialStockEvent 
              (BatchId, Delta,         UnitId,      ProjectId, TypeId, Note, UserId, EventGroupingKey, EventDt)
	SELECT aba.BatchId, bam.Available, bam.UnitId, @projectId, @eventTypeId,  N'Automaticky - Odpad z výroby', @robotUserId, LEFT(CAST(NEWID() AS NVARCHAR(255)), 32), GETDATE()
	  FROM vwAbandonedBatches aba
	  JOIN vwBatchAvailableAmount bam ON (bam.BatchId = aba.BatchId)
	 WHERE aba.ProjectId = @projectId
	   AND aba.Autofinalize = 1
	   AND aba.IsAbandoned = 1
END