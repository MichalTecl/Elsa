CREATE OR ALTER VIEW vw_ProductWeightIndex
AS
SELECT oi.PlacedName, oi.Weight / oi.Quantity Weight
  FROM OrderItem oi
  JOIN (SELECT soi.PlacedName, MAX(soi.Id) LatestId
          FROM OrderItem soi
		  WHERE soi.Weight is not null
		 GROUP BY soi.PlacedName) l ON (oi.PlacedName = l.PlacedName AND l.LatestId = oi.Id)