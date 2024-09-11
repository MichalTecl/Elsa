IF OBJECT_ID('dbo.xrep_productSales') IS NOT NULL
	DROP PROCEDURE dbo.xrep_productSales

GO

CREATE PROCEDURE [dbo].[xrep_productSales] (@projectId INT)
AS
BEGIN
	/*Title: Prodané počty produktů po měsících*/

	--DECLARE @projectId INT = 2;

WITH cte 
AS
(
	SELECT hm.Offset, oi.PlacedName, ISNULL(SUM(oi.quantity), 0) Qty
	  FROM PurchaseOrder po
	  JOIN vwOrderItems  poi ON (po.Id = poi.OrderId)
	  JOIN OrderItem     oi  ON (poi.OrderItemId = oi.Id)
	  JOIN vwHistoryMonths hm ON (po.BuyDate BETWEEN hm.StartDt AND hm.EndDt)	  
	 WHERE po.OrderStatusId = 5
	   AND po.ProjectId = @projectId
	   AND NOT EXISTS(SELECT TOP 1 1 FROM OrderItem child WHERE child.KitParentId = oi.Id)
	   AND hm.Offset > -13
	GROUP BY hm.Offset, oi.PlacedName
)
SELECT c.PlacedName "Produkt",     		
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -0) ,0) AS INT) "auto:MONTH(-0)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -1) ,0) AS INT) "auto:MONTH(-1)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -2) ,0) AS INT) "auto:MONTH(-2)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -3) ,0) AS INT) "auto:MONTH(-3)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -4) ,0) AS INT) "auto:MONTH(-4)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -5) ,0) AS INT) "auto:MONTH(-5)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -6) ,0) AS INT) "auto:MONTH(-6)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -7) ,0) AS INT) "auto:MONTH(-7)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -8) ,0) AS INT) "auto:MONTH(-8)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -9) ,0) AS INT) "auto:MONTH(-9)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -10) ,0) AS INT) "auto:MONTH(-10)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -11) ,0) AS INT) "auto:MONTH(-11)",
	CAST(ISNULL((SELECT Qty FROM cte WHERE cte.PlacedName = c.PlacedName AND Offset = -12) ,0) AS INT) "auto:MONTH(-12)"

	FROM (SELECt DISTINCT PlacedName FROM cte) c
ORDER BY c.PlacedName

END
GO


