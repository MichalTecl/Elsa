IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = N'autosp_SetBatchesFullSpent')
	DROP PROCEDURE autosp_SetBatchesFullSpent;

GO

CREATE PROCEDURE autosp_SetBatchesFullSpent (@projectId INT)
AS
BEGIN
	
	BEGIN TRANSACTION;

	DISABLE TRIGGER ALL ON MaterialBatch;

	SELECT TOP 1 1 FROM MaterialBatch WITH (TABLOCKX, HOLDLOCK);

	UPDATE mb
	SET FullSpendDt = GETDATE()
	FROM MaterialBatch mb
	WHERE EXISTS (
		SELECT 1
		FROM (
			SELECT mb.MaterialId, mb.BatchNumber
			FROM MaterialBatch mb
			JOIN vwBatchAvailableAmount bam ON mb.Id = bam.BatchId
			JOIN vwBatchEvent bet ON bet.BatchId = mb.Id
			WHERE mb.FullSpendDt IS NULL
			  AND mb.ProjectId = @projectId
			GROUP BY mb.MaterialId, mb.BatchNumber
			HAVING SUM(bam.Available) = 0
			   AND MAX(bet.EventDt) < DATEADD(month, -3, GETDATE())
		) t
		WHERE mb.MaterialId = t.MaterialId
		  AND mb.BatchNumber = t.BatchNumber
	);

	ENABLE TRIGGER ALL ON MaterialBatch;

	COMMIT TRANSACTION;
	
END



