CREATE PROCEDURE GetMaterialResolution (
	@projectId INT,
	@materialId INT)
AS
BEGIN
	DECLARE @avail TABLE (BatchId INT, UnitId INT, Available DECIMAL(19,5));
	INSERT INTO @avail 
	exec CalculateBatchUsages 1, null, @materialId;

	SELECT b.Id BatchId, b.BatchNumber, avail.Available, avail.UnitId, b.Created 
	  FROM @avail avail
	  JOIN MaterialBatch b ON (b.Id = avail.BatchId)
	 WHERE avail.Available > 0
	   AND b.CloseDt IS NULL
	ORDER BY b.Created;
END
  


