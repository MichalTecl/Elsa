IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'CRM_GetDistributorReport')
	DROP PROCEDURE CRM_GetDistributorReport;

GO

CREATE PROCEDURE CRM_GetDistributorReport(@customerId INT)
AS
BEGIN


	DECLARE @customerIds TABLE (Id int);

	-- TODO we need more sophisticated stuff here - emails are changing etc :(
	INSERT INTO @customerIds
	SELECT DISTINCT byMail.Id
	  FROM Customer src
	  JOIN Customer byMail ON (src.Email = byMail.Email OR src.Name = byMail.Name)
	 WHERE src.Id = @customerId;

	IF OBJECT_ID('tempdb..#custOrders', 'U') IS NOT NULL
		DROP TABLE #custOrders;

	SELECT oc.OrderId id INTO #custOrders
	  FROM vwOrderCustomer oc 
	  JOIN PurchaseOrder po ON (oc.OrderId = po.Id)
	 WHERE oc.CustomerId IN (SELECT Id FROM @customerIds)
	   AND po.OrderStatusId = 5
	   AND po.PurchaseDate >= DATEADD(year, -2, GETDATE());

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


	/********************** 1. Uvodni data VO *********************************************************/
	DECLARE @customerName NVARCHAR(300) = (SELECT TOP 1 c.Name
											 FROM Customer c
											 JOIN @customerIds src ON (src.Id = c.Id)
											WHERE c.Name IS NOT NULL
											ORDER BY c.ErpUid, --C.., P.., X...
													 c.Id DESC);

	DECLARE @salesRepName NVARCHAR(300) = (SELECT TOP 1 sr.PublicName
											 FROM SalesRepresentative sr
											 JOIN SalesRepCustomer sc ON (sr.Id = sc.SalesRepId)
											 JOIN @customerIds cus ON (sc.CustomerId = cus.Id)
											WHERE sr.PublicName IS NOT NULL);


	DECLARE @groupName NVARCHAR(300);
	DECLARE @marginText NVARCHAR(300);

	SELECT TOP 1 @groupName = cgm.ReportingName, @marginText = cgm.MarginText
	  FROM CustomerGroup cg
	  JOIN CustomerGroupMapping cgm ON (cg.ErpGroupName = cgm.GroupErpName)
	  JOIN @customerIds cids ON (cids.Id = cg.CustomerId)
	ORDER BY cgm.MarginPriority DESC;

	DECLARE @email NVARCHAR(300);
	DECLARE @phone NVARCHAR(300);
	DECLARE @companyId NVARCHAR(300);
	DECLARE @invAddress NVARCHAR(500);
	DECLARE @delAddress NVARCHAR(500);

	SELECT TOP 1 
			@companyId = po.CompanyRegId,
			@email = po.CustomerEmail,
			@phone = ISNULL(invAddr.Phone, delAddr.Phone),
			@invAddress = invAddr.Street + ' ' + invAddr.DescriptiveNumber + '/' + invAddr.OrientationNumber + ', ' + invAddr.Zip +', ' + invAddr.City,
			@delAddress = delAddr.Street + ' ' + delAddr.DescriptiveNumber + '/' + delAddr.OrientationNumber + ', ' + delAddr.Zip +', ' + delAddr.City
	  FROM PurchaseOrder po  
	  JOIN #custOrders co ON (po.Id = co.id)  
	  LEFT JOIN Address invAddr ON (po.InvoiceAddressId = invAddr.Id)
	  LEFT JOIN Address delAddr ON (po.DeliveryAddressId = delAddr.Id)
	ORDER BY po.Id DESC;  

	/********** DATASET 1 - Informace o VO partnerovi ********************************************/

	SELECT K,V
	FROM (
		SELECT 'Název VO partnera' K, @customerName V, 1 S UNION
		SELECT 'OZ', @salesRepName, 2 S UNION
		SELECT N'Zařazení ke dni ' + CONVERT(varchar(10), GETDATE(), 104), @groupName, 3 S UNION
		SELECT N'Marže', @marginText, 4 S UNION
		SELECT N'E-Mail', @email, 5 S UNION
		SELECT N'Telefon', @phone, 6 S UNION
		SELECT N'IČO', @companyId, 7 S UNION
		SELECT N'Fakturační adresa', @invAddress, 8 S UNION
		SELECT N'Doručovací adresa', @delAddress, 9 S) x
	ORDER BY x.S;



	/********* DATASET 2 - vsechny objednavky partnera *********************************************************/

	SELECT YEAR(po.PurchaseDate) Rok, MONTH(po.PurchaseDate) Měsíc, po.OrderNumber, itemsPrice.price MOC,  po.Price - priceElements.ttl VOC
	  FROM #custOrders co
	  JOIN PurchaseOrder po ON (co.Id = po.Id)
	  JOIN (SELECT oi.PurchaseOrderId, SUM(oi.TaxedPrice / (1 + (oi.TaxPercent / 100))) price
			  FROM OrderItem oi
			 GROUP BY oi.PurchaseOrderId) itemsPrice ON (po.Id = itemsPrice.PurchaseOrderId)
	  JOIN (SELECT pe.PurchaseOrderId, SUM(pe.Price) ttl
				FROM vwPriceElements pe
				WHERE pe.TypeName NOT IN ('percent_discount')
				GROUP BY pe.PurchaseOrderId) priceElements ON (priceElements.PurchaseOrderId = po.Id)

	ORDER BY YEAR(po.PurchaseDate) DESC, MONTH(po.PurchaseDate) DESC;

	/******** DATASET 3 - prehled objednavek dle produktu *********************************************************/

	DECLARE @twoYearsAgo DATETIME = DATEADD(year, -2, GETDATE());




	DECLARE @sql NVARCHAR(max) = '
	SELECT *
	FROM (
		SELECT M, Produkt, Qty
		FROM (
			SELECT CONVERT(varchar(7), po.PurchaseDate, 111) AS M, oip.EshopName AS Produkt, oi.Quantity AS Qty
			FROM #custOrders co
			JOIN PurchaseOrder po ON co.id = po.Id
			JOIN OrderItem oi ON co.id = oi.PurchaseOrderId
			JOIN vwOrderItemProduct oip ON oi.Id = oip.OrderItemId        
		) x
	) src
	PIVOT (
		SUM(Qty)
		FOR M IN ('+@monthTitles+')
	) pvt;';

	EXEC(@sql);
	SET @sql = '';

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