IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'UpdatePureBatchPriceCalculation')
	DROP PROCEDURE UpdatePureBatchPriceCalculation;
	
GO

CREATE PROCEDURE UpdatePureBatchPriceCalculation
AS
BEGIN
	DELETE FROM PureBatchPriceCalculation
	WHERE BatchId IN (
	SELECT x.BatchId
	  FROM PureBatchPriceCalculation clc
	  JOIN MaterialStockEvent mse ON (clc.BatchId = mse.BatchId)
	  CROSS APPLY dbo.GetAllBatchesContainingProvidedBatch(clc.BatchId) x 
	  WHERE mse.EventDt > clc.CalcDt);

	WHILE(1 = 1)
	BEGIN
    
		WITH link 
		AS
		(
			SELECT mbc.ComponentId, mbc.CompositionId, mbc.Volume * su.Multiplier Amount
			  FROM MaterialBatchComposition mbc
			  JOIN vwSmallestUnit su ON (mbc.UnitId = su.SourceUnitId)  
		)
		INSERT INTO PureBatchPriceCalculation
		SELECT Id, Amount, Price, Price / NULLIF(Amount, 0) PricePerUnit, GETDATE()
		FROM (
			SELECT mb.Id,
				   (mb.Volume * su.Multiplier)  - ISNULL(discards.amount, 0) Amount, 
				   ISNULL(mb.Price, 0) + ISNULL(mb.ProductionWorkPrice, 0) + ISNULL(components.price, 0) Price		   
			  FROM MaterialBatch mb
			  JOIN vwSmallestUnit su ON (su.SourceUnitId = mb.UnitId)
			  LEFT JOIN (SELECT mse.BatchId, SUM(mse.Delta * su.Multiplier) amount
						   FROM MaterialStockEvent mse
						   JOIN vwSmallestUnit su ON (mse.UnitId = su.SourceUnitId)
                           WHERE mse.TypeId = 1 -- jen ODPAD
						  GROUP BY mse.BatchId
						   ) discards ON (discards.BatchId = mb.Id)

			  LEFT JOIN (SELECT ln.CompositionId, SUM(ln.Amount * comp.PricePerUnit) price
						   FROM link ln
						   LEFT JOIN PureBatchPriceCalculation comp ON (ln.ComponentId = comp.BatchId)
						   GROUP BY ln.CompositionId) components ON (components.CompositionId = mb.Id)
				      
		  
			   WHERE mb.Id NOT IN (SELECT ex.BatchId FROM PureBatchPriceCalculation ex)
		   
			   /* neexistuje zadna komponeta teto kompozice, ktera by nebyla v @batches */
			   AND NOT EXISTS(SELECT TOP 1 1 
								  FROM MaterialBatchComposition mbc 
								  LEFT JOIN PureBatchPriceCalculation b ON (b.BatchId = mbc.ComponentId)
								  WHERE mbc.CompositionId = mb.Id 
									AND b.BatchId IS NULL)
			  ) x;

			IF (@@ROWCOUNT = 0)
				BREAK;
	END   
END
