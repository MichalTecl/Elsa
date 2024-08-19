IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'xrep_salesB2bB2c')
	DROP PROCEDURE xrep_salesB2bB2c;

GO

CREATE PROCEDURE xrep_salesB2bB2c (@projectId INT)
AS
BEGIN
    /*Title: Měsíční tržby B2B i B2C  */

	/* Note: 
Sumy měsíčních tržeb rozdělené podle typu zákazníka (B2B, B2C) + podíl typu na celkových tržbách v procentech.
Tržby jsou bez DPH a bez ceny dopravy a platby.

	
	*/
   
	WITH allOrders
	AS
	(
		SELECT YEAR(po.BuyDate) y, MONTH(po.BuyDate) m, (po.PriceWithVat - po.TaxedShippingCost - po.TaxedPaymentCost) / 1.21 p
		, (SELECT TOP 1 xc.IsDistributor FROM Customer xc WHERE xc.Id = oc.CustomerId) distributor
		FROM PurchaseOrder po	
		JOIN vwOrderCustomer oc ON (oc.OrderId = po.Id)	
		WHERE po.OrderStatusId = 5	
	),
	aggr
	AS
	(
	SELECT ao.y, ao.m, ao.distributor, SUM(ao.p) p
	  FROM allOrders ao 
	GROUP BY ao.y, ao.m, ao.distributor
	)
	SELECT timeline.y [Rok], 
		   timeline.m [Měsíc], 
		   CAST(b2b.p + b2c.p AS INT) [Celkem], 
		   CAST(b2b.p AS INT) [B2B], 
		   CAST(b2c.p AS INT) [B2C],
		   CAST(ROUND(b2b.p / (b2b.p + b2c.p) * 100, 0) As INT) [B2B%],
		   CAST(ROUND(b2c.p / (b2b.p + b2c.p) * 100, 0) As INT) [B2C%]
	  FROM (SELECT DISTINCT y, m FROM aggr) timeline
	  LEFT JOIN aggr b2b ON (b2b.y = timeline.y AND b2b.m = timeline.m AND b2b.distributor = 1)
	  LEFT JOIN aggr b2c ON (b2c.y = timeline.y AND b2c.m = timeline.m AND b2c.distributor = 0)
	ORDER BY timeline.y, timeline.m
			
END			


