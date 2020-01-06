IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'GetStockReport')
BEGIN
	DROP PROCEDURE GetStockReport;
END

GO

CREATE PROCEDURE GetStockReport
(
	@projectId INT,
	@culture VARCHAR(16),
	@reportDate DATETIME
)
AS
BEGIN
	DECLARE @levels TABLE (BatchId INT, UnitId INT, Available DECIMAL(19, 5));

	INSERT INTO @levels
	EXEC CalculateBatchUsages @projectId, NULL, NULL, @reportDate, 0;
		
	SELECT mi.Name as Sklad, 
	       m.Name as Materiál, 
		   mb.BatchNumber + '.' + TRIM(STR(mb.Id)) as Šarže, 
		   l.Available, 
		   mu.Symbol,
		   dbo.GetBatchPrice(mb.Id, @projectId, l.Available, l.UnitId) as Hodnota 
	  FROM @levels l
	  JOIN MaterialBatch mb ON (l.BatchId = mb.Id)
	  JOIN Material m ON (mb.MaterialId = m.Id)
	  JOIN MaterialInventory mi ON (m.InventoryId = mi.Id)
	  JOIN MaterialUnit mu ON (l.UnitId = mu.Id)
	 WHERE l.Available > 0
	   AND ISNULL(mb.IsHiddenForAccounting, 0) <> 1
	ORDER BY mi.Name, m.Name, mb.BatchNumber + '.' + TRIM(STR(mb.Id));


	 
	
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'GetBatchPricesReport')
BEGIN
	DROP PROCEDURE GetBatchPricesReport;
END

GO

CREATE PROCEDURE GetBatchPricesReport
(
	@projectId INT,
	@culture VARCHAR(16)
)
AS
BEGIN
	
	DECLARE @bids TABLE (batchId INT);

	INSERT INTO @bids
	SELECT b.Id
	  FROM MaterialBatch b
	 WHERE b.CloseDt IS NULL
	   AND b.ProjectId = @projectId
	   AND NOT EXISTS(SELECT TOP 1 1 FROM BatchPriceComponent bpc WHERE bpc.BatchId = b.Id);

	WHILE(1 = 1)
	BEGIN
		DECLARE @b INT = (SELECT TOP 1 batchId FROM @bids);
		IF (@b IS NULL) BREAK;

		EXEC GetBatchPriceComponents @b, @projectId, @culture, 1;

		DELETE FROM @bids WHERE batchId = @b;
	END
	
	SELECT m.Name, b.BatchNumber + '.' + TRIM(STR(b.Id)), bpc.Txt, bpc.Val
		   FROM BatchPriceComponent bpc
		   JOIN MaterialBatch b ON (bpc.BatchId = b.Id)
		   JOIN Material m ON (b.MaterialId = m.Id)
		 WHERE bpc.Val > 0 
		   AND b.ProjectId = @projectId
		   AND b.CloseDt IS NULL
	ORDER BY m.Name, b.BatchNumber + '.' + TRIM(STR(b.Id)), bpc.Id  ;	   
	
END

GO

-- EXEC GetBatchPricesReport 1, 'cs-cz'