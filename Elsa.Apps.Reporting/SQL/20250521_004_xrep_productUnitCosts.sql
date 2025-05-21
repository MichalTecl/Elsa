IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'xrep_productUnitCostsWithBatches')
	DROP PROCEDURE xrep_productUnitCostsWithBatches;

GO

CREATE PROCEDURE xrep_productUnitCostsWithBatches(@projectId INT)
AS
BEGIN

	/* Title: PŘÍMÉ náklady na vyrobený kus - jednotlivé šarže */

	/* Note: Náklady na vyrobený kus, které neobsahují podíl nepřímých nákladů. Konečná hodnota vzniká součtem nákladů všech komponent, bere se v úvahu odpad (tj. šarže 100ks, cena 200kč, odpad 50ks => 4 kč/ks)
Pokud má některá šarže ve složení zadánu cenu práce, započítává se také.
Report používá šarže vyrobené v tomto a minulém kalendářním roce.
	*/

	EXEC UpdatePureBatchPriceCalculation;

	SELECT m.Name, mb.BatchNumber, FORMAT(MAX(mb.Created),'dd.MM.yyyy')  Vytvoreno, ROUND(SUM(pbpc.Price) / NULLIF(SUM(pbpc.Amount), 0), 2) "Cena/Ks"
	  FROM MaterialBatch mb
	  JOIN PureBatchPriceCalculation pbpc ON (mb.Id = pbpc.BatchId)
	  JOIN Material m ON (mb.MaterialId = m.Id)
	  WHERE m.InventoryId = 3
	 GROUP BY m.Name, mb.BatchNumber
	 HAVING YEAR(MAX(mb.Created)) >= (YEAR(GETDATE()) - 1)
	 ORDER BY m.Name, MAX(mb.Created);

END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'xrep_productUnitCostsPerMaterial')
	DROP PROCEDURE xrep_productUnitCostsPerMaterial;

GO

GO

CREATE PROCEDURE xrep_productUnitCostsPerMaterial(@projectId INT)
AS
BEGIN

	/* Title: PŘÍMÉ náklady na vyrobený kus - šarže agregovány */

	/* Note: Náklady na vyrobený kus, které neobsahují podíl nepřímých nákladů. Konečná hodnota vzniká součtem nákladů všech komponent, bere se v úvahu odpad (tj. šarže 100ks, cena 200kč, odpad 50ks => 4 kč/ks)
Pokud má některá šarže ve složení zadánu cenu práce, započítává se také.
Report používá šarže vyrobené v tomto a minulém kalendářním roce.
	*/

    EXEC UpdatePureBatchPriceCalculation;

	SELECT m.Name, ROUND(SUM(pbpc.Price) / NULLIF(SUM(pbpc.Amount), 0), 2) "Cena/Ks"
		  FROM MaterialBatch mb
		  JOIN PureBatchPriceCalculation pbpc ON (mb.Id = pbpc.BatchId)
		  JOIN Material m ON (mb.MaterialId = m.Id)
		  WHERE m.InventoryId = 3
			AND YEAR(mb.Created) >= (YEAR(GETDATE()) - 1)
		 GROUP BY m.Name
		 ORDER BY  m.Name;

END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'xrep_product_margin')
	DROP PROCEDURE xrep_product_margin;

GO

CREATE PROCEDURE [xrep_product_margin] (@projectId INT)
AS
BEGIN 
	/*Title: Výrobky - Marže*/
	/* Note: 
Report používá data za posledních 12 měsíců.
Sloupce:
	Náklady/ks = průměrné výrobní náklady na kus daného produktu (nezapočítávají se NEPŘÍMÉ náklady!)
	Cena/ks bez dph = průměrná prodejní cena za posledních 12 měsíců bez slev
	Marže % = (Cena/ks bez dph -  Náklady/ks) /  Cena/ks bez dph * 100	

	*/

	EXEC UpdatePureBatchPriceCalculation;
		

	SELECT x.Product Produkt, 
		   AVG(x.costPerItem) "Náklady/ks", 
		   AVG(prices.PriceWoTax) "Cena/ks bez dph",
		   CAST(ROUND((AVG(prices.PriceWoTax) - AVG(x.costPerItem)) / NULLIF(AVG(prices.PriceWoTax), 0) * 100, 0) As INT) AS "Marže %"
	  FROM (
		SELECT m.Name Product, m.Id MaterialId, mb.BatchNumber Batch, MAX(mb.Created) Created, SUM(bup.PricePerUnit * bup.Amount) / NULLIF(SUM(bup.Amount), 0) CostPerItem 
		  FROM MaterialBatch mb  
		  JOIN Material      m ON (mb.MaterialId = m.Id)
		  JOIN MaterialInventory mi ON (m.InventoryId = mi.Id)
		  JOIN PureBatchPriceCalculation bup ON (mb.Id = bup.BatchId)
	  
		 WHERE mi.Id = 3
		GROUP BY m.Name, m.Id, mb.BatchNumber	
	 ) x
	 LEFT JOIN (SELECT mb.MaterialId, AVG((oi.TaxedPrice / oi.Quantity) - (oi.TaxedPrice / oi.Quantity * oi.TaxPercent / 100)) PriceWoTax
			  FROM OrderItem oi
			  JOIN OrderItemMaterialBatch oimb ON (oimb.OrderItemId = oi.Id)
			  JOIN MaterialBatch mb ON (mb.Id = oimb.MaterialBatchId)
			  WHERE DATEDIFF(month, mb.Created, GETDATE()) <= 12
				AND oi.KitParentId IS NULL
			 GROUP BY mb.MaterialId) prices ON (x.MaterialId = prices.MaterialId)
	 WHERE DATEDIFF(month, x.Created, GETDATE()) <= 12
	 GROUP BY x.Product
 ORDER BY x.Product;

END


GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'xrep_product_unit_costs')
	DROP PROCEDURE xrep_product_unit_costs;

GO
