IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'crmfilter_neverOrderedAProduct')
	DROP PROCEDURE crmfilter_neverOrderedAProduct;

GO

CREATE PROCEDURE crmfilter_neverOrderedAProduct(@productNameLike varchar(100))
AS
BEGIN
    /* Title:Nikdy neobjednali produkt */
	/* Note:Vybere zákazníky, kteří nikdy neobjednali určitý produkt. Produkt zadávejte jako název v E-Shopu. Je možné použít hvězdičky '*' pro proměnnou část názvu. 
Příklady: 
- "100% přírodní deodorant * (malý)" hledá všechny malé deodoranty. 
- "Balzám na rty*" vyhledá všechny druhy balzámu na rty. 
- "*dárek*" vyhledá všechny produkty, které obsahují slovo "dárek"  */

	/* @productNameLike.control: /UI/DistributorsApp/FilterControls/TextBox.html */
	/* @productNameLike.label: Název produktu */

	-- todo escape "%" in original string
	SET @productNameLike = REPLACE(@productNameLike, '*', '%');

	SELECT c.Id
	  FROM Customer c
	WHERE c.IsCompany = 1
      AND c.IsDistributor = 1
	  AND NOT EXISTS(SELECT TOP 1 1
	                   FROM PurchaseOrder po
					   JOIN OrderItem oi ON (oi.PurchaseOrderId = po.Id)
					   WHERE po.CustomerErpUid = c.ErpUid
					     AND oi.PlacedName like @productNameLike)


END
GO