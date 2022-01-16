IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'GetAvailableBatchesByNrLike')
	DROP PROCEDURE GetAvailableBatchesByNrLike;

GO

CREATE PROCEDURE GetAvailableBatchesByNrLike(@projectId INT, @materialId INT, @batchNrLike  NVARCHAR(64))
AS
BEGIN
	SELECT mb.BatchNumber
	  FROM vwBatchAvailableAmount bam
	  JOIN MaterialBatch          mb  ON (bam.BatchId = mb.Id)
	 WHERE bam.MaterialId = @materialId
	   AND bam.ProjectId = @projectId	   
	   AND bam.Available > 0
	   AND mb.BatchNumber LIKE @batchNrLike;
END