--
-- DROP TABLE HlpBatchUnitPrice
--CREATE TABLE HlpBatchUnitPrice (BatchId INT NOT NULL UNIQUE, PricePerUnit DECIMAL(19, 4) NOT NULL, CalcDt DATETIME NOT NULL);

DELETE FROM HlpBatchUnitPrice 
  WHERE BatchId IN (
	SELECT hlup.BatchId
	  FROM HlpBatchUnitPrice hlup 
	  JOIN MaterialStockEvent mse ON (mse.BatchId = hlup.BatchId AND mse.EventDt > hlup.CalcDt));

DECLARE @toAdd TABLE(BatchId INT);
WHILE (1=1)
BEGIN
	
	DELETE FROM @toAdd;
	
	-- Budeme opakovat pro vsechny sarze, ktere uz maji spocitane ceny komponent:
	INSERT INTO @toAdd
	SELECT DISTINCT mb.Id
		FROM MaterialBatch mb
		WHERE EXISTS(SELECT 1 FROM BatchPriceComponent bpc WHERE bpc.BatchId = mb.Id) -- uz je zahrnuto v ucetnich datech 
		AND NOT EXISTS(SELECT 1 FROM HlpBatchUnitPrice bp WHERE bp.BatchId = mb.Id)       -- jeste neni spocitano
   
		-- uz jsou spocitane ceny vsech komponent
		AND NOT EXISTS(SELECT 1 FROM MaterialBatchComposition mbc
								WHERE mbc.CompositionId = mb.Id
								AND mbc.ComponentId NOT IN (SELECT BatchId FROM HlpBatchUnitPrice));

	IF NOT EXISTS(SELECT TOP 1 1 FROM @toAdd)
	BEGIN
		-- Uz je hotovo
		BREAK;
	END
	
	WHILE(1=1)
	BEGIN
		DECLARE @currBatchId INT = (SELECT TOP 1 BatchId FROM @toAdd);

		IF (@currBatchId IS NULL)
			BREAK;

		DELETE FROM @toAdd WHERE BatchId = @currBatchId;


		DECLARE @thePrice DECIMAL(19, 4);
		
		-- 1. prace, nakup, fixni naklady etc...
		SET @thePrice = ISNULL((SELECT SUM(bpc.Val) FROM BatchPriceComponent bpc WHERE bpc.BatchId = @currBatchId AND bpc.SourceBatchId IS NULL), 0);

		-- 2. cena komponent
		SET @thePrice = @thePrice + ISNULL((
		SELECT SUM(convertedAmount.Value * bpc.PricePerUnit)
		  FROM MaterialBatchComposition mbc
		  CROSS APPLY dbo.ConvertToSmallestUnit(mbc.Volume, mbc.UnitId) convertedAmount
		  JOIN HlpBatchUnitPrice bpc ON (mbc.ComponentId = bpc.BatchId)
		 WHERE mbc.CompositionId = @currBatchId), 0);

		 IF EXISTS(SELECT 1
		             FROM MaterialBatch mb
					 JOIN Material m ON (mb.MaterialId = m.Id)
					WHERE mb.Id = @currBatchId
					  AND m.InventoryId = 3)
         BEGIN
			-- toto je uz konecny vyrobek, jehoz cena na ks neni ovlivnena odpadem:
			INSERT INTO HlpBatchUnitPrice
			SELECT TOP 1 @currBatchId, CASE WHEN bt.TotalVolume > 0 THEN @thePrice / bt.TotalVolume ELSE 0 END, GETDATE()
			  FROM vwBatchThrash bt
			 WHERE bt.BatchId = @currBatchId;
		END
		ELSE
		BEGIN
		   -- toto je nejaka hmota nebo smes, kde cenu na jednotku ovlivnuje odpad:
			INSERT INTO HlpBatchUnitPrice
			SELECT TOP 1 @currBatchId, CASE WHEN bt.TotalVolume > bt.ThrashedVolume THEN @thePrice / (bt.TotalVolume - bt.ThrashedVolume) ELSE 0 END, GETDATE()
			  FROM vwBatchThrash bt
			 WHERE bt.BatchId = @currBatchId;
        END
	END
END


SELECT m.Name, mb.BatchNumber, MAX(mb.Created), SUM(bup.PricePerUnit * mb.Volume) / SUM(mb.Volume)
  FROM MaterialBatch mb  
  JOIN Material      m ON (mb.MaterialId = m.Id)
  JOIN MaterialInventory mi ON (m.InventoryId = mi.Id)
  LEFT JOIN HlpBatchUnitPrice bup ON (mb.Id = bup.BatchId)
 WHERE mi.Id = 3
GROUP BY m.Name, mb.BatchNumber
ORDER BY m.Name, mb.BatchNumber, MAX(mb.Created);