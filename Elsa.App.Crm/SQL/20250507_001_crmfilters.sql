IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'crmfilter_orderedProductInLastXMonths')
	DROP PROCEDURE crmfilter_orderedProductInLastXMonths;

GO

CREATE PROCEDURE crmfilter_orderedProductInLastXMonths(@productNameLike nvarchar(100), @strMonths varchar(100),  @filterText nvarchar(1000) OUTPUT)
AS
BEGIN
    /* Title:Objednali produkt v posledních X měsících */
	/* Note:Vybere zákazníky, kteří v období posledních X měsíců objednali určitý produkt. Produkt zadávejte jako název v E-Shopu. Je možné použít hvězdičky '*' pro proměnnou část názvu. 
Příklady: 
- "100% přírodní deodorant * (malý)" hledá všechny malé deodoranty. 
- "Balzám na rty*" vyhledá všechny druhy balzámu na rty. 
- "*dárek*" vyhledá všechny produkty, které obsahují slovo "dárek"  */

	/* @productNameLike.control: /UI/DistributorsApp/FilterControls/TextBox.html */
	/* @productNameLike.label: Název produktu */

	/* @strMonths.control: /UI/DistributorsApp/FilterControls/NumberInput.html */
	/* @strMonths.label: Počet měsíců dozadu*/

	declare @months INT = CAST(@strMonths AS INT);
	
	declare @dtFrom DATETIME;
	SET @dtFrom = GETDATE();
	SEt @dtFrom = DATEADD(month, -1 * @months, GETDATE());


	SET @filterText = 'Objednali od ' + FORMAT(@dtFrom, 'dd.MM. yy');

	-- todo escape "%" in original string
	SET @productNameLike = REPLACE(@productNameLike, '*', '%');
		
	SELECT c.Id
	  FROM Customer c
	WHERE c.IsCompany = 1
      AND c.IsDistributor = 1
	  AND EXISTS(SELECT TOP 1 1
	                   FROM PurchaseOrder po
					   JOIN OrderItem oi ON (oi.PurchaseOrderId = po.Id)
					   WHERE po.CustomerErpUid = c.ErpUid
					     AND po.BuyDate >= @dtFrom	
					     AND oi.PlacedName like @productNameLike
						 )


END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'crmfilter_hasCategory')
	DROP PROCEDURE crmfilter_hasCategory;

GO

CREATE PROCEDURE crmfilter_hasCategory(@categoryName varchar(100), @filterText nvarchar(1000) OUTPUT)
AS
BEGIN
    /* Title:Mají kategorii */
	/* Note:Vybere zákazníky, kteří patří do zadané kategorie  */

	/* @categoryName.control: /UI/DistributorsApp/FilterControls/CustomerGroupSelect.html */
	/* @categoryName.label: Kategorie */

	SET @filterText = N'Mají kategorii ' + @categoryName;
		
	SELECT c.Id
	  FROM Customer c
	  JOIN CustomerGroup cg ON (cg.CustomerId = c.Id)
	WHERE c.IsCompany = 1
      AND c.IsDistributor = 1
	  AND cg.ErpGroupName = @categoryName;  


END
GO
IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'crmfilter_hasSalesRep')
	DROP PROCEDURE crmfilter_hasSalesRep;

GO

CREATE PROCEDURE crmfilter_hasSalesRep(@srName varchar(100), @filterText nvarchar(1000) OUTPUT)
AS
BEGIN
    /* Title:Patří OZ */
	/* Note:Vybere zákazníky, kteří patří zadanému OZ */

	/* @srName.control: /UI/DistributorsApp/FilterControls/SalesRepSelect.html */
	/* @srName.label: OZ */

	SET @filterText = N'Patří OZ ' + @srName;
		
	SELECT c.Id
	  FROM Customer c
	  JOIN SalesRepCustomer src ON (src.CustomerId = c.Id)
	  JOIN SalesRepresentative sr ON (sr.Id = src.SalesRepId)
	WHERE c.IsCompany = 1
      AND c.IsDistributor = 1
	  AND sr.PublicName = @srName;  


END
GO