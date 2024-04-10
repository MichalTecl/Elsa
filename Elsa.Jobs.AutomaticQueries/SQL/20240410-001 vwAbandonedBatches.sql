IF EXISTS(SELECT TOP 1 1 FROM sys.views WHERe name = 'vwAbandonedBatches')
	DROP VIEW vwAbandonedBatches;

GO

CREATE VIEW vwAbandonedBatches
AS
SELECT x.*, 
       DATEDIFF(day, x.EventDt, GETDATE()) DaysFromEvent,
	   CASE WHEN DATEDIFF(day, x.EventDt, GETDATE()) >= x.DaysBeforeWarnForUnused THEN 1 ELSE 0 END IsAbandoned
  FROM (
	SELECT m.ProjectId, 
		   mb.Id BatchId, 
		   m.Id MaterialId, 
		   mb.BatchNumber, 
		   m.Name MaterialName, 
		   m.DaysBeforeWarnForUnused, 
		   ISNULL(m.UseAutofinalization, 0) Autofinalize, 
		   ISNULL(m.UsageProlongsLifetime, 0) UsageProlongsLifetime, 
		   ISNULL(le.LastEvent, mb.Created) EventDt
	  FROM Material m
	  JOIN MaterialBatch mb ON (m.Id = mb.MaterialId)
	  JOIN vwBatchAvailableAmount bam ON (mb.Id = bam.BatchId)
	  LEFT JOIN (SELECT be.BatchId, MAX(be.EventDt) LastEvent
				   FROM vwBatchEvent be 
				  WHERE be.EventName IN ('DIRECT_SALE_ALLOCATION', 'DIRECT_SALE_RETURN', 'USED_AS_COMPONENT', 'ESHOP_SALE')
				 GROUP BY be.BatchId) le ON (m.UsageProlongsLifetime = 1 AND le.BatchId = mb.Id)
	 WHERE m.DaysBeforeWarnForUnused IS NOT NULL
	   AND mb.CloseDt IS NULL
	   AND bam.Available > 0) x





   