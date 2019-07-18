IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_fakeBatch')
	DROP PROCEDURE sp_fakeBatch;
GO 

CREATE PROCEDURE sp_fakeBatch @materialId INT, @qty DECIMAL, @authorId INT
AS
BEGIN

	DECLARE @newBatchId INT;

	INSERT INTO MaterialBatch (MaterialId, Volume,  UnitId, AuthorId, Note, Price, ProjectId, Created, IsAvailable, AllStepsDone)
	SELECT TOP 1                     m.Id,   @qty,  m.NominalUnitId, @authorId, '', 0, 1, GETDATE(), 1, 1
	FROM Material m
	WHERE m.Id = @materialId;

	SET @newBatchId = SCOPE_IDENTITY();

	UPDATE MaterialBatch 
	   SET BatchNumber = 'FAKE_' + (SELECT TOP 1 Name FROM Material WHERE Id = @materialId) + '_' + CAST(@newBatchId AS VARCHAR)
	 WHERE Id = @newBatchId;

	SELECT TOP 1 * FROM MaterialBatch WHERE Id = @newBatchId;
  	
	findUnresolvedStep:

		DECLARE @requiredMaterialId INT = NULL;
		DECLARE @missingStepId INT;
		DECLARE @requiredQty DECIMAL;

		SELECT TOP 1 @requiredMaterialId = mpsm.MaterialId, @missingStepId = mps.Id, @requiredQty = mpsm.Amount * @qty
		   FROM MaterialProductionStep mps
		   JOIN MaterialProductionStepMaterial mpsm ON (mpsm.StepId = mps.Id)
		  WHERE mps.MaterialId = @materialId
			AND NOT EXISTS (SELECT TOP 1 1 FROM BatchProductionStep bps WHERE bps.StepId = mps.Id AND bps.BatchId = @newBatchId);

		IF (@requiredMaterialId IS NULL)
		BEGIN
		  RETURN;
		END	

		EXEC sp_fakeBatch @requiredMaterialId, @requiredQty, @authorId;

		DECLARE @stepBatchId INT;

		SELECT TOP 1 @stepBatchId = Id 
		FROM MaterialBatch mb
		WHERE mb.AuthorId = @authorId
		  AND mb.MaterialId = @requiredMaterialId
		  AND mb.Volume >= @requiredQty
        ORDER BY mb.Id DESC;

		IF (@stepBatchId IS NULL)
		BEGIN
			SELECT 'Cannot resolve step';
			RETURN;
		END

		INSERT INTO BatchProductionStep (StepId, ProducedAmount, ConfirmUserId, ConfirmDt, WorkerId, BatchId)
		VALUES (                 @missingStepId,           @qty,     @authorId, GETDATE(), @authorId, @newBatchId);

		INSERT INTO BatchProuctionStepSourceBatch 
		                      (StepId, SourceBatchId, UnitId, UsedAmount)
		SELECT TOP 1 SCOPE_IDENTITY(),  @stepBatchId, m.NominalUnitId, @requiredQty
		  FROM Material m
		WHERE m.Id = @requiredMaterialId;

	GOTO findUnresolvedStep;

END

