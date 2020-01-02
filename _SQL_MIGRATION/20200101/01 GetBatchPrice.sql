
IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'GetBatchPrice')
BEGIN
	DROP FUNCTION GetBatchPrice;
END

GO

CREATE FUNCTION GetBatchPrice
(
	@batchId INT,
	@projectId INT,
	@amount DECIMAL(19, 5),
	@amountUnit INT
)
RETURNS DECIMAL(19, 4)
AS
BEGIN
	DECLARE @batchTotal DECIMAL(19, 5);
	DECLARE @batchCreationDt DATETIME;
	DECLARE @cogsInventoryId INT;

	DECLARE @price DECIMAL(19,5);

	SELECT @price = ISNULL(Price, 0) + ISNULL(ProductionWorkPrice, 0), 
		   @batchTotal = dbo.ConvertToUnit(@projectId, Volume, UnitId, @amountUnit),       
		   @batchCreationDt = ISNULL(Produced, Created),
		   @cogsInventoryId = mi.Id
	 FROM MaterialBatch mb
	 JOIN Material m ON (mb.MaterialId = m.Id)
	 LEFT JOIN MaterialInventory mi ON (m.InventoryId = mi.Id AND ISNULL(mi.IncludesFixedCosts, 0) = 1)
	WHERE mb.Id = @batchId;
	
	SET @price = @price + ISNULL(
								(SELECT SUM(dbo.GetBatchPrice (mbc.ComponentId, @projectId, mbc.Volume, mbc.UnitId))
								  FROM MaterialBatchComposition mbc
								 WHERE mbc.CompositionId = @batchId), 0);

	IF (@cogsInventoryId IS NOT NULL)
	BEGIN
		DECLARE @totalProducedWithFixedCost DECIMAL(19,5) = ISNULL((
		SELECT SUM(mb.Volume)
		  FROM MaterialBatch mb
		  JOIN Material m ON (mb.MaterialId = m.Id)
		  JOIN MaterialInventory mi ON (m.InventoryId = mi.Id)
		WHERE mi.IncludesFixedCosts = 1
		  AND YEAR(mb.Created) = YEAR(@batchCreationDt)
		  AND MONTH(mb.Created) = MONTH(@batchCreationDt)), 1);

		SET @price = @price + ISNULL(( 		
			SELECT SUM((fcv.Value / 100 * fct.PercentToDistributeAmongProducts) / @totalProducedWithFixedCost)
			  FROM FixedCostType fct
			  JOIN FixedCostValue fcv ON (fcv.FixedCostTypeId = fct.Id)
			WHERE fcv.Year = YEAR(@batchCreationDt)
			  AND fcv.Month = MONTH(@batchCreationDt)), 0) * @batchTotal;
	END
	
	IF (@batchTotal <> @amount)
	BEGIN
		SET @price = @price * (@amount / @batchTotal);
	END

	RETURN @price;
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'FormatAmount')
BEGIN
	DROP FUNCTION FormatAmount;
END

GO

CREATE FUNCTION FormatAmount
(		
	@amount DECIMAL(19, 5),
	@amountUnit INT,
	@culture NVARCHAR(16)
)
RETURNS NVARCHAR(100)
AS
BEGIN
	RETURN  FORMAT(ROUND(@amount, 2) , 'N2', @culture) + ' ' + (SELECT TOP 1 Symbol FROM MaterialUnit WHERE Id = @amountUnit);
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'GetBatchPriceComponents')
BEGIN
	DROP PROCEDURE GetBatchPriceComponents;
END

GO

CREATE PROCEDURE GetBatchPriceComponents
(
	@batchId INT,
	@projectId INT,
	@culture VARCHAR(16)
)
AS
BEGIN
	DECLARE @result TABLE (Id INT IDENTITY(1,1), Txt NVARCHAR(1000), Val DECIMAL(19,5), IsWarn BIT);
	DECLARE @batchTotal DECIMAL(19, 5);
	DECLARE @batchCreationDt DATETIME;
	DECLARE @cogsInventoryId INT;

	DECLARE @purchPrice DECIMAL(19,5);
	DECLARE @workPrice DECIMAL(19, 5);
	DECLARE @sourceCurrencyPrice DECIMAL(19, 5);
	DECLARE @sourceCurrencySymbol NVARCHAR(16);
						
	SELECT @purchPrice = ISNULL(Price, 0), 
	       @workPrice = ISNULL(ProductionWorkPrice, 0),       
		   @batchCreationDt = ISNULL(Produced, Created),
		   @cogsInventoryId = mi.Id,
		   @sourceCurrencyPrice = cc.SourceValue,
		   @sourceCurrencySymbol = srcu.Symbol
	 FROM MaterialBatch mb
	 JOIN Material m ON (mb.MaterialId = m.Id)
	 LEFT JOIN MaterialInventory mi ON (m.InventoryId = mi.Id AND ISNULL(mi.IncludesFixedCosts, 0) = 1)
	 LEFT JOIN CurrencyConversion cc ON (mb.PriceConversionId = cc.Id)
	 LEFT JOIN Currency srcu ON (cc.SourceCurrencyId = srcu.Id) 
	WHERE mb.Id = @batchId;

	INSERT INTO @result (Txt, Val)
	SELECT N'Nákupní cena', @purchPrice WHERE @purchPrice > 0 AND @sourceCurrencyPrice IS NULL UNION
	SELECT N'Nákupní cena (Konverze ' + FORMAT(ROUND(@sourceCurrencyPrice, 2) , 'N2', @culture) + ' ' + @sourceCurrencySymbol + N')', @purchPrice WHERE @sourceCurrencyPrice IS NOT NULL UNION
	SELECT N'Cena práce', @workPrice    WHERE @workPrice > 0;

	INSERT INTO @result (Txt, Val)
	-- 33 kg Jedlá soda š. 123456
	SELECT dbo.FormatAmount(mbc.Volume, mbc.UnitId, @culture) + ' ' + m.Name + N' šarže ' + mb.BatchNumber, dbo.GetBatchPrice(mb.Id, @projectId, mb.Volume, mb.UnitId)
	  FROM MaterialBatchComposition mbc
	  JOIN MaterialBatch mb ON (mbc.ComponentId = mb.Id)
	  JOIN Material      m  ON (m.Id = mb.MaterialId)
	 WHERE mbc.CompositionId = @batchId;
	 	
	IF (@cogsInventoryId IS NOT NULL)
	BEGIN				
		DECLARE @totalProducedWithFixedCost DECIMAL(19,5) = ISNULL((
		SELECT SUM(mb.Volume)
		  FROM MaterialBatch mb
		  JOIN Material m ON (mb.MaterialId = m.Id)
		  JOIN MaterialInventory mi ON (m.InventoryId = mi.Id)
		WHERE mi.IncludesFixedCosts = 1
		  AND YEAR(mb.Created) = YEAR(@batchCreationDt)
		  AND MONTH(mb.Created) = MONTH(@batchCreationDt)), 1);

		DECLARE @fixComponent DECIMAL(19, 5) = 	
			((SELECT SUM((fcv.Value / 100 * fct.PercentToDistributeAmongProducts) / @totalProducedWithFixedCost)
			  FROM FixedCostType fct
			  JOIN FixedCostValue fcv ON (fcv.FixedCostTypeId = fct.Id)
			WHERE fcv.Year = YEAR(@batchCreationDt)
			  AND fcv.Month = MONTH(@batchCreationDt)) * @batchTotal);

		IF (@fixComponent IS NULL)
		BEGIN
			INSERT INTO @result (txt, Val, IsWarn)
			VALUES (N'Nepřímé náklady nedostupné pro ' + TRIM(STR(MONTH(@batchCreationDt))) + '/' + TRIM(STR(YEAR(@batchCreationDt))), 0, 1);
		END
		ELSE
		BEGIN
			INSERT INTO @result (txt, Val)
			VALUES (N'Nepřímé náklady ' + TRIM(STR(MONTH(@batchCreationDt))) + '/' + TRIM(STR(YEAR(@batchCreationDt))), @fixComponent);
		END
	END
	
	SELECT Txt, Val, ISNULL(IsWarn, 0) as IsWarn
	  FROM @result
    ORDER BY Id;	
END

GO





