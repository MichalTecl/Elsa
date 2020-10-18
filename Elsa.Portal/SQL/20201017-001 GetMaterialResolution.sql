ALTER PROCEDURE [dbo].[GetMaterialResolution] (
	@projectId INT,
	@materialId INT,
	@createdBefore DATETIME,
	@ignoreExistenceOfThisBatch INT)
AS
BEGIN
    /*
	DECLARE @avail TABLE (BatchId INT, UnitId INT, Available DECIMAL(19,5));
	INSERT INTO @avail 
	exec CalculateBatchUsages 1, null, @materialId;
	
	IF (@ignoreExistenceOfThisBatch IS NOT NULL)
	BEGIN
	    INSERT INTO @avail
		SELECT mbc.ComponentId, mbc.UnitId, mbc.Volume
		  FROM MaterialBatchComposition mbc
		  JOIN MaterialBatch component ON component.Id = mbc.ComponentId
         WHERE component.MaterialId = @materialId
		   AND mbc.CompositionId = @ignoreExistenceOfThisBatch;
	END

	SELECT b.Id BatchId, b.BatchNumber, avail.Available, avail.UnitId, b.Created 
	  FROM @avail avail
	  JOIN MaterialBatch b ON (b.Id = avail.BatchId)
	 WHERE avail.Available > 0
	   AND b.Created <= ISNULL(@createdBefore, GETDATE())
	   AND b.CloseDt IS NULL
	ORDER BY b.Created;
	*/

	SELECT *
	  FROM 
	  (	
		SELECT b.Id BatchId, b.BatchNumber, bam.Available, bam.UnitId, b.Created 
		  FROM vwBatchAvailableAmount bam
		  JOIN MaterialBatch          b  ON (bam.BatchId = b.Id)
		 WHERE bam.ProjectId = @projectId
		   AND bam.MaterialId = @materialId
		   AND bam.Available > 0
		   AND b.CloseDt IS NULL
		   AND ((@createdBefore IS NULL) OR (b.Created <= @createdBefore))		
		UNION		
			SELECT mbc.ComponentId BatchId, component.BatchNumber, mbc.Volume, mbc.UnitId, component.Created
		      FROM MaterialBatchComposition mbc
		      JOIN MaterialBatch component ON component.Id = mbc.ComponentId
             WHERE component.MaterialId = @materialId
		       AND mbc.CompositionId = @ignoreExistenceOfThisBatch
		) x
		ORDER BY x.Created
	   
	   

END
  
GO


