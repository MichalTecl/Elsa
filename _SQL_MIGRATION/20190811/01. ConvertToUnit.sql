IF object_id('ConvertToUnit', 'FN') IS NOT NULL
BEGIN
    DROP FUNCTION [dbo].[ConvertToUnit]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION ConvertToUnit 
(
	@ProjectId INT,
	@Value DECIMAL(19,4),
	@SourceUnitId INT,
	@TargetUnitId INT
)
RETURNS DECIMAL(19,4)
AS
BEGIN
	IF (@SourceUnitId = @TargetUnitId)
	BEGIN
		RETURN @Value;
	END


	RETURN @Value * (SELECT TOP 1 Multiplier FROM UnitConversion WHERE ProjectId = @ProjectId AND SourceUnitId = @SourceUnitId AND TargetUnitId = @TargetUnitId);
END
GO

