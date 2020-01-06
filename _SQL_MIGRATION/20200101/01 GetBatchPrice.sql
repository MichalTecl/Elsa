
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

IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE name = 'BatchPriceComponent')
BEGIN
	
	CREATE TABLE BatchPriceComponent
	(
		Id BIGINT IDENTITY(1,1),
		BatchId INT NOT NULL,
		Txt NVARCHAR(1000) NOT NULL, 
		Val DECIMAL(19,5) NOT NULL, 
		IsWarn BIT, 
		SourceBatchId INT
	);

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
		   @sourceCurrencySymbol = srcu.Symbol,
		   @batchTotal = mb.Volume
	 FROM MaterialBatch mb
	 JOIN Material m ON (mb.MaterialId = m.Id)
	 LEFT JOIN MaterialInventory mi ON (m.InventoryId = mi.Id AND ISNULL(mi.IncludesFixedCosts, 0) = 1)
	 LEFT JOIN CurrencyConversion cc ON (mb.PriceConversionId = cc.Id)
	 LEFT JOIN Currency srcu ON (cc.SourceCurrencyId = srcu.Id) 
	WHERE mb.Id = @batchId;

	INSERT INTO BatchPriceComponent (BatchId, Txt, Val)
	SELECT @batchId, N'Nákupní cena', @purchPrice WHERE @purchPrice > 0 AND @sourceCurrencyPrice IS NULL UNION
	SELECT @batchId, N'Nákupní cena (Konverze ' + FORMAT(ROUND(@sourceCurrencyPrice, 2) , 'N2', @culture) + ' ' + @sourceCurrencySymbol + N')', @purchPrice WHERE @sourceCurrencyPrice IS NOT NULL UNION
	SELECT @batchId, N'Cena práce', @workPrice    WHERE @workPrice > 0;

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
			INSERT INTO BatchPriceComponent (BatchId, Txt, Val, IsWarn)
			VALUES (@batchId, N'Nepřímé náklady nedostupné pro ' + TRIM(STR(MONTH(@batchCreationDt))) + '/' + TRIM(STR(YEAR(@batchCreationDt))), 0, 1);
		END
		ELSE
		BEGIN
			INSERT INTO BatchPriceComponent (BatchID, txt, Val)
			VALUES (@batchId, N'Nepřímé náklady ' + TRIM(STR(MONTH(@batchCreationDt))) + '/' + TRIM(STR(YEAR(@batchCreationDt))), @fixComponent);
		END
	END

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

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'GetAllBatchesContainingProvidedBatch')
BEGIN
	DROP FUNCTION [dbo].[GetAllBatchesContainingProvidedBatch] ;
END

GO 

CREATE FUNCTION [dbo].[GetAllBatchesContainingProvidedBatch] 
(	
	@batchId INT
)
RETURNS TABLE 
AS
RETURN
WITH rs0 AS (
	SELECT ComponentId, CompositionId
	FROM MaterialBatchComposition
	WHERE ComponentId = @batchId
UNION ALL
SELECT P.ComponentId, P.CompositionId
FROM rs0 AS C 
INNER JOIN MaterialBatchComposition AS P ON C.CompositionId = P.ComponentId
)
SELECT
	r.ComponentId as BatchId
FROM
	rs0 r
	join MaterialBatch Composition on r.CompositionId = Composition.Id
	join Material m on Composition.MaterialId = m.Id
UNION	
	SELECT @batchId as BatchId;
GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'PrecalculatePriceComponents')
BEGIN
	DROP PROCEDURE PrecalculatePriceComponents;
END

GO

CREATE PROCEDURE PrecalculatePriceComponents
(
	@projectId INT,
	@culture VARCHAR(16),
	@year INT,
	@month INT
)
AS
BEGIN
	SELECT 1;
END

GO
IF EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE name = 'OnFixedCostsChanged')
BEGIN
	DROP PROCEDURE OnFixedCostsChanged;
END

GO 

CREATE PROCEDURE OnFixedCostsChanged (@projectId INT, @year INT, @month INT)
AS
BEGIN
	
	WHILE(1=1)
	BEGIN
		DECLARE @bid INT = (SELECT TOP 1 b.Id
		                      FROM MaterialBatch b
							  JOIN Material m ON (m.Id = b.MaterialId)
							  JOIN MaterialInventory mi ON (m.InventoryId = mi.Id)
							  JOIN BatchPriceComponent bpc ON (b.Id = bpc.BatchId)
							 WHERE b.ProjectId = @projectId
							   AND mi.IncludesFixedCosts = 1
							   AND YEAR(b.Created) = @year
							   AND MONTH(b.Created) = @month); 
		
		IF (@bid IS NULL)
		BEGIN
			RETURN;
		END

		EXEC OnBatchChanged @bid;

	END

END

GO

IF EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE name = 'OnBatchChanged')
BEGIN
	DROP PROCEDURE OnBatchChanged;
END

GO 

CREATE PROCEDURE OnBatchChanged
(
	@batchId INT
)
AS
BEGIN
  DELETE FROM BatchPriceComponent WHERE BatchId IN (SELECT BatchId FROM dbo.GetAllBatchesContainingProvidedBatch(@batchID));
   
  DECLARE @batchDt DATETIME;
  DECLARE @projectId INT;
  SELECT TOP 1 @batchDt = b.Created, @projectId = b.ProjectId
        FROM MaterialBatch b
		JOIN Material m ON (b.MaterialId = m.Id)
		JOIN MaterialInventory i ON (m.InventoryId = i.Id AND ISNULL(i.IncludesFixedCosts, 0) = 1);
  
  IF (@batchDt IS NOT NULL)		
  BEGIN
	DECLARE @y INT = YEAR(@batchDt), @m INT = MONTH(@batchDt);
    EXEC OnFixedCostsChanged @projectId, @y, @m; 
  END		  
END

GO

IF EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[TRG_BatchChanged]'))
	DROP TRIGGER [dbo].[TRG_BatchChanged];

GO

CREATE TRIGGER TRG_BatchChanged ON MaterialBatch FOR INSERT, UPDATE, DELETE 
AS
WHILE EXISTS(SELECT TOP 1 1 FROM inserted)
BEGIN
	DECLARE @processed TABLE (id INT);
	DECLARE @id INT;
	WHILE(1=1)
	BEGIN
		SET @id = (SELECT TOP 1 i.Id FROM inserted i WHERE i.Id NOT IN (SELECT p.Id FROM @processed p));
		IF (@id IS NULL)
		BEGIN
			RETURN;
		END
		EXEC OnBatchChanged @id;
		INSERT INTO @processed VALUES (@id);
	END	
END

GO 

IF EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[TRG_FixedCostChanged]'))
	DROP TRIGGER [dbo].[TRG_FixedCostChanged];

GO

CREATE TRIGGER TRG_FixedCostChanged ON FixedCostValue FOR INSERT, UPDATE, DELETE 
AS
WHILE EXISTS(SELECT TOP 1 1 FROM inserted)
BEGIN
	DECLARE @processed TABLE (id INT);
	DECLARE @id INT;
	DECLARE @month INT;
	DECLARE @year INT;
	DECLARE @projectId INT;

	WHILE(1=1)
	BEGIN
		SET @id = NULL;
		SELECT TOP 1 @id = i.Id, @month = i.Month, @year = i.Year, @projectId = i.ProjectId 
		  FROM inserted i 
		 WHERE i.Id NOT IN (SELECT p.id FROM @processed p);

		 IF (@id IS NULL)
		 BEGIN
			RETURN;
		 END

		 INSERT INTO @processed VALUES (@id);
	END
END

GO

IF EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE name = 'PreloadBatchPrices')
	DROP PROCEDURE PreloadBatchPrices;

GO

CREATE PROCEDURE PreloadBatchPrices
(
	@projectId INT,
	@from DATETIME,
	@to DATETIME,
	@culture VARCHAR(16),
	@silent BIT = 0
)
AS
BEGIN
	DECLARE @bids TABLE(id int);

	INSERT INTO @bids
	SELECT b.Id
	  FROM MaterialBatch b
	 WHERE b.ProjectId = @projectId
	   AND b.Created >= @from
	   AND b.Created <= @to
	   AND NOT EXISTS(SELECT TOP 1 1 
	                    FROM BatchPriceComponent bpc
					   WHERE bpc.BatchId = b.Id);

	 WHILE (1=1)
	 BEGIN	
		DECLARE @id INT = (SELECT TOP 1 id FROM @bids);
		IF (@id IS NULL)
		BEGIN
			BREAK;
		END

		EXEC GetBatchPriceComponents @id, @projectId, @culture, 1;

		DELETE FROM @bids WHERE id = @id;
	 END

	 IF (@silent = 0)
	 BEGIN
		 SELECT  bpc.BatchId, bpc.Txt, bpc.Val, ISNULL(bpc.IsWarn, 0), bpc.SourceBatchId
		   FROM BatchPriceComponent bpc
		   JOIN MaterialBatch b ON (bpc.BatchId = b.Id)
		 WHERE b.ProjectId = @projectId
		   AND b.Created >= @from
		   AND b.Created <= @to
		 ORDER BY bpc.Id;
	 END
END