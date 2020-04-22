ALTER VIEW [dbo].[vwUnitConversion]
AS
	WITH cte AS (
	SELECT uc.ProjectId, uc.SourceUnitId, uc.TargetUnitId, uc.Multiplier  
		FROM UnitConversion uc   
	UNION
		SELECT uc.ProjectId, uc.TargetUnitId as SourceUnitId, uc.SourceUnitId as TargetUnitId, 1/uc.Multiplier  
		FROM UnitConversion uc 
	UNION
		SELECT mu.ProjectId, mu.Id SourceUnitId, mu.Id TargetUnitId, 1 as Multiplier
  		FROM MaterialUnit mu)
	SELECT c.ProjectId, c.SourceUnitId, c.TargetUnitId, c.Multiplier, preferred.TargetUnitId _PreferredTargetUnitId, preferred.Multiplier _MultiplierToPreferredTargetUnit
	  FROM cte c	
      JOIN (SELECT sub.SourceUnitId src, MAX(sub.Multiplier) maxMul
	          FROM cte sub
			GROUP BY sub.SourceUnitId) maxmul ON (maxmul.src = c.SourceUnitId)
      LEFT JOIN cte preferred ON (preferred.SourceUnitId = c.SourceUnitId 
	                          AND preferred.Multiplier = maxMul.maxMul
							  AND preferred.TargetUnitId <> c.SourceUnitId)