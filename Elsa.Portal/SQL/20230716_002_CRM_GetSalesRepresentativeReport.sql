IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'CRM_GetSalesRepresentativeReport')
	DROP PROCEDURE CRM_GetSalesRepresentativeReport;

GO

CREATE PROCEDURE CRM_GetSalesRepresentativeReport(@salesRepId INT, @startDt DATETIME, @endDt DATETIME)
AS
BEGIN

	IF OBJECT_ID('tempdb..#custOrders', 'U') IS NOT NULL
		DROP TABLE #custOrders;

	SELECT DISTINCT po.Id INTO #custOrders
	  FROM SalesRepCustomer src
	  JOIN vwOrderCustomer oc ON (src.CustomerId = oc.CustomerId)
	  JOIN PurchaseOrder po ON (oc.OrderId = po.Id)
	 WHERE GETDATE() BETWEEN src.ValidFrom ANd ISNULL(src.ValidTo, GETDATE())
	   AND po.OrderStatusId = 5
	   AND po.PurchaseDate >= DATEADD(year, -3, GETDATE());
	
	DECLARE @customers TABLE (id INT);
	INSERT INTO @customers 
	SELECT DISTINCT oc.CustomerId
	 FROM #custOrders po 
	 JOIN vwOrderCustomer oc ON (po.Id = oc.OrderId);

	DECLARE @months TABLE (i int identity(1,1), symbol NVARCHAR(7));

	WITH NumbersCTE AS (
		SELECT GETDATE() AS Dt
		UNION ALL
		SELECT DATEADD(Month, -1, Dt) as Dt
		FROM NumbersCTE
		WHERE Dt > DATEADD(year, -2, GETDATE())
	)
	INSERT INTO @months (Symbol)
	SELECT CONVERT(varchar(7), Dt, 111) Symbol
	FROM NumbersCTE OPTION (MAXRECURSION 0);

	DECLARE @monthTitles VARCHAR(2000);
	SELECT @monthTitles = COALESCE(@monthTitles + ', ', '') + QUOTENAME(m.symbol)
	FROM @months m
	ORDER BY m.i

	/******** DATASET 1: Přehled všech VO partnerů - s porovnáním za minulá období */

	DECLARE @monthsdiff INT = DATEDIFF(month, @startDt, @endDt);

	DECLARE @prevPeriodStartDt DATETIME = DATEADD(month, @monthsDiff, @startDt);
	DECLARE @prevPeriodEndDt DATETIME = DATEADD(month, @monthsDiff, @endDt);

	DECLARE @prevYearPerStart DATETIME = DATEADD(year, -1, @startDt);
	DECLARE @prevYearPerEnd   DATETIME = DATEADD(year, -1, @endDt);

	DECLARe @ozName NVARCHAR(300) = (SELECT TOP 1 PublicName FROM SalesRepresentative WHERE Id = @salesRepId);
	SET @ozName = ISNULL(@ozName, N'Všichni');

	SELECT K, V FROM
	(SELECT 1 S, 'Generováno dne' K, CONVERT(varchar(10), GETDATE(), 104) V UNION
	SELECT 2 S, 'OZ', @ozName) x
	ORDER BY S;

	SELECT Title, CONVERT(varchar(7), St, 111) S, CONVERT(varchar(7), En, 111) E
	FROM
		(SELECT 1 S, N'Sledované' Title , @startDt St, @endDt En UNION
		SELECT 2 S, N'Předchozí kalendářně' Title, @prevPeriodStartDt St, @prevPeriodEndDt En UNION
		SELECT 3 S, N'Předchozí sezónně' Title, @prevYearPerStart St, @prevYearPerEnd En) x
	ORDER BY x.S;
	
	WITH cte
	AS 
	(
		SELECT x.CustomerId,
			   x.Obd,
			   SUM(x.VOC) VOC,
			   SUM(x.MOC) MOC
		FROM (
			SELECT c.CustomerId,
				CASE 
					WHEN (po.PurchaseDate BETWEEN @startDt ANd @endDt)                     THEN 'Sledovane'
					WHEN (po.PurchaseDate BETWEEN @prevPeriodStartDt AND @prevPeriodEndDt) THEN 'MinuleKal'
					WHEN (po.PurchaseDate BETWEEN @prevYearPerStart AND @prevYearPerEnd)   THEN 'MinuleSez'
					ELSE NULL
				END Obd,	
			po.PurchaseDate, po.OrderNumber, itemsPrice.price MOC,  po.Price - priceElements.ttl VOC
			  FROM #custOrders co
			  JOIN PurchaseOrder po ON (co.Id = po.Id)
			  JOIN vwOrderCustomer c ON (c.OrderId = po.Id)
			  JOIN (SELECT oi.PurchaseOrderId, SUM(oi.TaxedPrice / (1 + (oi.TaxPercent / 100))) price
					  FROM OrderItem oi
					 GROUP BY oi.PurchaseOrderId) itemsPrice ON (po.Id = itemsPrice.PurchaseOrderId)
			  JOIN (SELECT pe.PurchaseOrderId, SUM(pe.Price) ttl
						FROM vwPriceElements pe
						WHERE pe.TypeName NOT IN ('percent_discount')
						GROUP BY pe.PurchaseOrderId) priceElements ON (priceElements.PurchaseOrderId = po.Id)
		 ) x WHERE x.Obd IS NOT NULL
		GROUP BY x.CustomerId,
			   x.Obd)
		SELECT sr.PublicName SalesRepName, 
		       COALESCE(c.Name, c.Email, LTRIM(STR(c.Id))) CustomerName, 
			   cgs.Groups CustomerGroup,
			   Sledovane.VOC VOC,
			   Sledovane.MOC MOC,
			   MinuleKal.MOC MOC_MINKAL,
			   MinuleSez.MOC MOC_MINSEZ
		  FROM (SELECT Id FROM @customers) customers
		  LEFT JOIN SalesRepCustomer src ON (customers.Id = src.CustomerId AND (GETDATE() BETWEEN src.ValidFrom AND ISNULL(src.ValidTo, GETDATE())))
		  LEFT JOIN SalesRepresentative sr ON (src.SalesRepId = sr.Id)
		  LEFT JOIN Customer c ON (customers.Id = c.Id)
		  LEFT JOIN vwCustomerGroupsCsv cgs ON (customers.Id = cgs.CustomerId)

		  LEFT JOIN cte Sledovane ON (Sledovane.CustomerId = c.Id AND Sledovane.Obd = 'Sledovane')
		  LEFT JOIN cte MinuleKal ON (MinuleKal.CustomerId = c.Id AND MinuleKal.Obd = 'MinuleKal')
		  LEFT JOIN cte MinuleSez ON (MinuleSez.CustomerId = c.Id AND MinuleSez.Obd = 'MinuleSez')
	    ORDER BY c.Name;



		DECLARE @sql NVARCHAR(MAX);
		/****** DATASET 4 - prehled objednavek dle SKUPIN produktu ***************/

	SET @sql = '
	SELECT *
	FROM (
		SELECT M, ISNULL(Skupina, '''') Skupina, Qty
		FROM (
				SELECT CONVERT(varchar(7), po.PurchaseDate, 111) AS M, rmg.Name AS Skupina, oi.Quantity AS Qty
				FROM #custOrders co
				JOIN PurchaseOrder po ON co.id = po.Id
				JOIN OrderItem oi ON co.id = oi.PurchaseOrderId
				JOIN vwOrderItemProduct oip ON oi.Id = oip.OrderItemId
				LEFT JOIN ReportingMaterialGroupMaterial rmgm ON (oip.MaterialId = rmgm.MaterialId)
				LEFT JOIN ReportingMaterialGroup rmg ON (rmgm.GroupId = rmg.Id)
				) x
	 ) src
	 PIVOT (
		SUM(Qty)
		FOR M IN ('+@monthTitles +')
	) pvt;';

	EXEC(@sql);

END