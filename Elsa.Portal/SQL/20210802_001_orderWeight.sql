IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'vw_ProductWeightIndex')
BEGIN 
	DROP VIEW vw_ProductWeightIndex;
END

GO

CREATE VIEW vw_ProductWeightIndex
AS
SELECT oi.PlacedName, oi.Weight / oi.Quantity Weight
  FROM OrderItem oi
  JOIN (SELECT soi.PlacedName, MAX(soi.Id) LatestId
          FROM OrderItem soi
		 GROUP BY soi.PlacedName) l ON (oi.PlacedName = l.PlacedName AND l.LatestId = oi.Id)

GO

IF NOT EXISTS(SELECT TOP 1 1 FROM SysConfig WHERE [Key] = 'OrderProcessing.UseWeight')
BEGIN
    INSERT INTO SysConfig (ProjectId, [Key], ValueJson, ValidFrom, InsertUserId)
     VALUES (1, 'OrderProcessing.UseWeight', 'false', GETDATE(), 2);
END