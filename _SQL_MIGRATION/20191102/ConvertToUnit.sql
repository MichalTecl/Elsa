USE [test]
GO

/****** Object:  UserDefinedFunction [dbo].[ConvertToUnit]    Script Date: 11/3/2019 1:37:34 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER FUNCTION [dbo].[ConvertToUnit] 
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


	RETURN @Value * (SELECT TOP 1 Multiplier FROM vwUnitConversion WHERE ProjectId = @ProjectId AND SourceUnitId = @SourceUnitId AND TargetUnitId = @TargetUnitId);
END
GO


