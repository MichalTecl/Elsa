IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'crmfilter_orderedProductInPeriod')
	DROP PROCEDURE crmfilter_orderedProductInPeriod;

GO

CREATE PROCEDURE crmfilter_orderedProductInPeriod(@productNameLike nvarchar(100), @from varchar(100), @to varchar(100),  @filterText nvarchar(1000) OUTPUT)
AS
BEGIN
    /* Title:Objednali produkt v období */
	/* Note:Vybere zákazníky, kteří v zadaném období objednali určitý produkt. Produkt zadávejte jako název v E-Shopu. Je možné použít hvězdičky '*' pro proměnnou část názvu. 
Příklady: 
- "100% přírodní deodorant * (malý)" hledá všechny malé deodoranty. 
- "Balzám na rty*" vyhledá všechny druhy balzámu na rty. 
- "*dárek*" vyhledá všechny produkty, které obsahují slovo "dárek"  */

	/* @productNameLike.control: /UI/DistributorsApp/FilterControls/TextBox.html */
	/* @productNameLike.label: Název produktu */

	/* @from.control: /UI/DistributorsApp/FilterControls/DateInput.html */
	/* @from.label: Od */

	/* @to.control: /UI/DistributorsApp/FilterControls/DateInput.html */
	/* @to.label: Do */


	SET @filterText = 'Objednali ' + @filterText;

	-- todo escape "%" in original string
	SET @productNameLike = REPLACE(@productNameLike, '*', '%');

	declare @dtFrom DATETIME;
	declare @dtTo DATETIME;

	BEGIN TRY
    SET @dtFrom = CAST(@from AS DATETIME);
	END TRY
	BEGIN CATCH
		THROW 50000, 'Neplatné datum v poli "Od".', 1;
	END CATCH;

	BEGIN TRY
		SET @dtTo = CAST(@to AS DATETIME);
	END TRY
	BEGIN CATCH
		THROW 50001, 'Neplatné datum v poli "Do".', 1;
	END CATCH;
	
	SELECT c.Id
	  FROM Customer c
	WHERE c.IsCompany = 1
      AND c.IsDistributor = 1
	  AND EXISTS(SELECT TOP 1 1
	                   FROM PurchaseOrder po
					   JOIN OrderItem oi ON (oi.PurchaseOrderId = po.Id)
					   WHERE po.CustomerErpUid = c.ErpUid
					     AND po.BuyDate >= @dtFrom
						 AND po.BuyDate <= @dtTo
					     AND oi.PlacedName like @productNameLike
						 )


END
GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'crmfilter_didntOrderProductInPeriod')
	DROP PROCEDURE crmfilter_didntOrderProductInPeriod;

GO

CREATE PROCEDURE crmfilter_didntOrderProductInPeriod(@productNameLike nvarchar(100), @from varchar(100), @to varchar(100),  @filterText nvarchar(1000) OUTPUT)
AS
BEGIN
    /* Title:Neobjednali produkt v období */
	/* Note:Vybere zákazníky, kteří v zadaném období neobjednali určitý produkt. Produkt zadávejte jako název v E-Shopu. Je možné použít hvězdičky '*' pro proměnnou část názvu. 
Příklady: 
- "100% přírodní deodorant * (malý)" hledá všechny malé deodoranty. 
- "Balzám na rty*" vyhledá všechny druhy balzámu na rty. 
- "*dárek*" vyhledá všechny produkty, které obsahují slovo "dárek"  */

	/* @productNameLike.control: /UI/DistributorsApp/FilterControls/TextBox.html */
	/* @productNameLike.label: Název produktu */

	/* @from.control: /UI/DistributorsApp/FilterControls/DateInput.html */
	/* @from.label: Od */

	/* @to.control: /UI/DistributorsApp/FilterControls/DateInput.html */
	/* @to.label: Do */


	SET @filterText = 'Neobjednali ' + @filterText;

	-- todo escape "%" in original string
	SET @productNameLike = REPLACE(@productNameLike, '*', '%');

	declare @dtFrom DATETIME;
	declare @dtTo DATETIME;

	BEGIN TRY
    SET @dtFrom = CAST(@from AS DATETIME);
	END TRY
	BEGIN CATCH
		THROW 50000, 'Neplatné datum v poli "Od".', 1;
	END CATCH;

	BEGIN TRY
		SET @dtTo = CAST(@to AS DATETIME);
	END TRY
	BEGIN CATCH
		THROW 50001, 'Neplatné datum v poli "Do".', 1;
	END CATCH;
	
	SELECT c.Id
	  FROM Customer c
	WHERE c.IsCompany = 1
      AND c.IsDistributor = 1
	  AND NOT EXISTS(SELECT TOP 1 1
	                   FROM PurchaseOrder po
					   JOIN OrderItem oi ON (oi.PurchaseOrderId = po.Id)
					   WHERE po.CustomerErpUid = c.ErpUid
					     AND po.BuyDate >= @dtFrom
						 AND po.BuyDate <= @dtTo
					     AND oi.PlacedName like @productNameLike
						 )


END
GO