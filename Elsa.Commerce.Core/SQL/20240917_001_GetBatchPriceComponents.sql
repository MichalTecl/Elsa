
IF EXISTS(SELECT * FROM sys.procedures WHERE Name = 'GetBatchPriceComponents')
	DROP PROCEDURE GetBatchPriceComponents;

GO

CREATE PROCEDURE GetBatchPriceComponents
(
	@batchId INT,
	@projectId INT,
	@culture VARCHAR(16),
	@silent BIT = 0
)
AS
BEGIN

	IF EXISTS(SELECT TOP 1 1 FROM BatchPriceComponent WHERE BatchId = @batchId)
	BEGIN		
		IF (@silent <> 1)
		BEGIN
			SELECT BatchId, Txt, Val, ISNULL(IsWarn, 0), SourceBatchId 
				FROM BatchPriceComponent
				WHERE BatchId = @batchId
			ORDER BY Id;
		END
		RETURN;		
	END
		
	DECLARE @batchTotal DECIMAL(19, 5);
	DECLARE @batchCreationDt DATETIME;

	DECLARE @purchPrice DECIMAL(19,5);
	DECLARE @workPrice DECIMAL(19, 5);
	DECLARE @sourceCurrencyPrice DECIMAL(19, 5);
	DECLARE @convRate DECIMAL(19, 5);
	DECLARE @sourceCurrencySymbol NVARCHAR(16);
	DECLARE @cogs DECIMAL(19, 5);

						
	SELECT @purchPrice = ISNULL(Price, 0), 
	       @workPrice = ISNULL(ProductionWorkPrice, 0),       
		   @batchCreationDt = ISNULL(Produced, Created),
		   @sourceCurrencyPrice = cc.SourceValue,
		   @sourceCurrencySymbol = srcu.Symbol,
		   @batchTotal = mb.Volume,
		   @cogs = ISNULL(fbc.Value, 0),
		   @convRate = ISNULL(cr.Rate, 1)
	 FROM MaterialBatch mb
	 LEFT JOIN CurrencyConversion cc ON (mb.PriceConversionId = cc.Id)
	 LEFT JOIN CurrencyRate cr ON (cc.CurrencyRateId = cr.Id)
	 LEFT JOIN Currency srcu ON (cc.SourceCurrencyId = srcu.Id) 
	 LEFT JOIN FixedCostBatchComponent fbc ON (mb.Id = fbc.BatchId)
	WHERE mb.Id = @batchId;

	INSERT INTO BatchPriceComponent (BatchId, Txt, Val)
	SELECT @batchId, N'Nákupní cena', @purchPrice WHERE @purchPrice > 0 AND @sourceCurrencyPrice IS NULL 
	UNION
	SELECT @batchId, N'Nákupní cena (Konverze ' + FORMAT(ROUND(@sourceCurrencyPrice, 2) , 'N2', @culture) + ' ' + @sourceCurrencySymbol + N' * kurz ' + FORMAT(ROUND(@convRate, 3) , 'N2', @culture) + ')', @purchPrice WHERE @sourceCurrencyPrice IS NOT NULL 
	UNION
	SELECT @batchId, N'Cena práce', @workPrice    WHERE @workPrice > 0 UNION
	SELECT @batchId, N'Nepřímé náklady ' 
	       + RIGHT('0' + TRIM(STR(MONTH(@batchCreationDt))), 2) + '/' + TRIM(STR(YEAR(@batchCreationDt))) , @cogs WHERE @cogs > 0;

	DECLARE @dependencies TABLE(did INT);
	INSERT INTO @dependencies
	SELECT DISTINCT ComponentId
	 FROM MaterialBatchComposition
	WHERE CompositionId = @batchId;

	WHILE EXISTS(SELECT TOP 1 1 FROM @dependencies)
	BEGIN
		DECLARE @did INT = (SELECT TOP 1 did FROM @dependencies);
		EXEC GetBatchPriceComponents @did, @projectId, @culture, 1;
		DELETE FROM @dependencies WHERE did = @did;
	END

	INSERT INTO BatchPriceComponent (BatchId, SourceBatchId, Txt, Val)
	SELECT @batchId, x.ComponentId, dbo.FormatAmount(x.ComponentAmount,x.ComponentUnit, @culture) + ' ' + x.MaterialName + N' šarže ' + x.ComponentBatchNr,  x.Price
	FROM (
		SELECT mbc.ComponentId, 
			   mbc.Volume ComponentAmount, 
			   mbc.UnitId ComponentUnit, 			    
			   cp.price  * (mbc.Volume / dbo.ConvertToUnit(@projectId, cb.Volume, cb.UnitId, mbc.UnitId)) Price,
			   cb.BatchNumber ComponentBatchNr,
			   cm.Name MaterialName
		  FROM MaterialBatchComposition mbc	  
		  JOIN MaterialBatch cb ON (mbc.ComponentId = cb.Id)
		  JOIN Material cm ON (cb.MaterialId = cm.Id)
		  LEFT JOIN (SELECT pc.BatchId, SUM(pc.Val) price
					   FROM BatchPriceComponent pc
					  GROUP BY pc.BatchId) cp ON (mbc.ComponentId = cp.BatchId)
		 WHERE mbc.CompositionId = @batchId
		   AND ISNULL(cb.IsHiddenForAccounting, 0) = 0) as x;

	IF NOT EXISTS(SELECT TOP 1 1 FROM BatchPriceComponent WHERE BatchId = @batchId)
	BEGIN
		INSERT INTO BatchPriceComponent (BatchId, Txt, Val, IsWarn)
			VALUES (@batchId, N'Nemá cenu', 0, 0);
	END
	
	IF (@silent <> 1)
	BEGIN
		SELECT BatchId, Txt, Val, ISNULL(IsWarn, 0), SourceBatchId 
			FROM BatchPriceComponent
			WHERE BatchId = @batchId
		ORDER BY Id;
	END

END


/*

delete 
  from BatchPriceComponent 
  where batchId in (select batchid from BatchPriceComponent bpc where bpc.txt like '%(Konverze%')

*/