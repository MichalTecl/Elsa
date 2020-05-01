IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'vwSmallestUnit')
BEGIN
	DROP VIEW vwSmallestUnit;
END

GO

CREATE VIEW vwSmallestUnit
AS
select distinct uc.SourceUnitId, ISNULL(uc._PreferredTargetUnitId, uc.SourceUnitId) PreferredUnitId, ISNULL(uc._MultiplierToPreferredTargetUnit, 1) Multiplier
  from vwUnitConversion uc;
