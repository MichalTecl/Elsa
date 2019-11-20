CREATE VIEW vwUnitConversion
AS
SELECT uc.ProjectId, uc.SourceUnitId, uc.TargetUnitId, uc.Multiplier  
  FROM UnitConversion uc   
UNION
 SELECT uc.ProjectId, uc.TargetUnitId as SourceUnitId, uc.SourceUnitId as TargetUnitId, 1/uc.Multiplier  
  FROM UnitConversion uc 
UNION
  SELECT mu.ProjectId, mu.Id SourceUnitId, mu.Id TargetUnitId, 1 as Multiplier
  	FROM MaterialUnit mu

	