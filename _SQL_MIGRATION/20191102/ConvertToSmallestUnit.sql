CREATE FUNCTION ConvertToSmallestUnit 
(
	@sourceValue DECIMAL(19,5),
	@sourceUnitId INT	 	
)
RETURNS TABLE
AS
RETURN
	SELECT @sourceValue * uc.Multiplier Value, uc.TargetUnitId
	  FROM MaterialUnit sourceUnit
	  JOIN (
			SELECT co.sourceUnitId, MAX(co.Multiplier) Multiplier
			  FROM vwUnitConversion co		
			GROUP BY co.ProjectId, co.sourceUnitId 
	  ) bestMatch ON (bestMatch.SourceUnitId = sourceUnit.Id)
	  JOIN vwUnitConversion uc ON (uc.SourceUnitId = bestMatch.SourceUnitId AND uc.Multiplier = bestMatch.Multiplier)
	WHERE sourceUnit.Id = @sourceUnitId;