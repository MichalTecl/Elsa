
ALTER PROCEDURE [dbo].[GetBatchPricesReport]
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
	
	SELECT m.Name, b.BatchNumber + '.' + TRIM(STR(b.Id)), 
	       bpc.Txt, 
		   bpc.Val,   
		   RIGHT('0' + TRIM(STR(MONTH(b.Created))), 2) + '/' + TRIM(STR(YEAR(b.Created))) as Vlozeno,
		   b.UnitId,
		   bpc.Val / b.Volume 
	FROM BatchPriceComponent bpc
		   JOIN MaterialBatch b ON (bpc.BatchId = b.Id)
		   JOIN Material m ON (b.MaterialId = m.Id)
		 WHERE bpc.Val > 0 
		   AND b.ProjectId = @projectId
		   AND b.CloseDt IS NULL
	ORDER BY m.Name, b.BatchNumber + '.' + TRIM(STR(b.Id)), bpc.Id  ;	   
	
END

GO


