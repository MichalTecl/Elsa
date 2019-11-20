
-- 0. create helper objects

IF NOT EXISTS(SELECT * FROM sys.tables WHERE name  = 'hlpMaterialRefactoringLog')
BEGIN
	CREATE TABLE hlpMaterialRefactoringLog (
		Id                        INT IDENTITY(1,1) NOT NULL, 
		FinalMaterialId           INT NOT NULL,
		DerivedMaterialId         INT NULL,
		MaterialProductionStepId  INT NOT NULL);
END

GO

IF EXISTS(SELECT * FROM sys.tables WHERE name = 'hlpBatchSplit')
BEGIN
	DROP TABLE hlpBatchSplit;
END

GO

CREATE TABLE hlpBatchSplit (originalBatchId INT, partialBatchId INT, PartialStartDt DATETIME);

GO

IF EXISTS(SELECT * FROM sys.tables WHERE name = 'hlpReallocations')
BEGIN
	DROP TABLE hlpReallocations;	
END

GO

CREATE TABLE hlpReallocations 
	(objectName NVARCHAR(100),
	 objectId BIGINT,
	 batchId INT,
	 amount DECIMAL(19, 5));

GO
IF EXISTS(SELECT * FROM sys.procedures WHERe name = 'sp_reallocate')
BEGIN
	DROP PROCEDURE sp_reallocate;
END

GO

CREATE PROCEDURE sp_reallocate 
(
	@objectName NVARCHAR(100),
	@objectId BIGINT,
	@maxBatchDt DATETIME,
	@originalBatchId INT,
	@amount DECIMAL(19,5)
)
AS
BEGIN
	
	DECLARE @dMatName NVARCHAR(200) = (SELECT TOP 1 Name FROM Material m WHERE m.Id = (SELECT MaterialId FROM MaterialBatch b WHERE b.Id = @originalBatchId));
			
    -- ' batches before ' + CAST(@maxBatchDt AS VARCHAR) +
	PRINT 'STARTING ' + @objectName + ' ID=' + TRIM(STR(@objectId)) + ' Qty=' + TRIM(STR(@amount)) + ' Material=' + @dMatName + ' OriginalBatchId=' + TRIM(STR(@originalBatchId)); 
		
	DECLARE @avails TABLE (BatchId INT, UnitId INT, Available DECIMAL(19, 5));

	DECLARE @resolutions TABLE (BatchId INT, Dt DATETIME);
	
	retryit:

	INSERT INTO @resolutions (BatchId, Dt)
	SELECT bs.partialBatchId, bs.PartialStartDt
	 FROM hlpBatchSplit bs
    WHERE bs.PartialStartDt <= ISNULL(@maxBatchDt, GETDATE())
	  AND bs.originalBatchId = @originalBatchId;

    DECLARE @dbgc INT = (SELECT COUNT(*) FROM @resolutions);
	PRINT '   Count of found partial batches=' + TRIM(STR(@dbgc));

	IF (ISNULL(@dbgc, 0) < 1)
	BEGIN		
		IF (@maxBatchDt IS NOT NULL)
		BEGIN
			PRINT '  Trying  time travel...';
			SET @maxBatchDt = NULL;
			GOTO retryit;
		END		
	END
		
	WHILE EXISTS(SELECT TOP 1 1 FROM @resolutions)
	BEGIN
		DECLARE @batchId INT = (SELECT TOP 1 BatchId FROM @resolutions ORDER BY Dt DESC);

		PRINT '   Remaining amount =' + TRIM(STR(@amount));
		PRINT '   Trying to use BatchId=' + TRIM(STR(@batchId));

		DELETE FROM @avails;

		INSERT INTO @avails
		EXEC CalculateBatchUsages 1, @batchId;
		

		SET @dbgc = (SELECT COUNT(*) FROM @avails);
		PRINT '   Received count of availability records=' + TRIM(STR(ISNULL(@dbgc, 0)));

		IF (ISNULL(@dbgc, 0) <> 1)
		BEGIN
			SELECT 'No availability for:', * FROM MaterialBatch WHERE Id = @batchId;
		END

		DECLARE @allocateHere DECIMAL(19, 5) = (SELECT Available FROM @avails);
		
		PRINT '   Procedure returned available=' + TRIM(STR(@allocateHere));
				
		SET @allocateHere = @allocateHere - ISNULL((SELECT SUM(amount) FROM hlpReallocations WHERE batchId = @batchId), 0);

		PRINT '   After correction available=' + TRIM(STR(@allocateHere));

		IF (@allocateHere > 0)
		BEGIN				
			IF (@allocateHere > @amount)
			BEGIN
				SET @allocateHere = @amount;
			END

			INSERT INTO hlpReallocations 
			SELECT @objectName, @objectId, @batchId, @allocateHere;

			SET @amount = @amount - @allocateHere;
		END

		IF (@amount = 0)
		BEGIN
			DELETE FROM @resolutions;
		END
		ELSE
		BEGIN				
			DELETE FROM @resolutions WHERE BatchId = @batchId;
        END
	END
	
	IF (@amount > 0)
	BEGIN
		IF (@maxBatchDt IS NOT NULL)
		BEGIN
			PRINT '  Trying  time travel...';
			SET @maxBatchDt = NULL;
			GOTO retryit;
		END	
		ELSE
		BEGIN
			DECLARE @m NVARCHAR(200) = ' Cannot reallocate ' + @objectName + ' ID=' + STR(@objectId);
			PRINT @m;
			THROW  51000, @m, 1; 	
		END
	END

	PRINT 'DONE ' + @objectName + ' ' + TRIM(STR(@objectId));
END
GO

EXEC sp_deallocateUnpackOrderBatches;

/****************************/

BEGIN TRAN;

/****************************/
-- DEBUG:

DECLARE @debugBatchId INT; -- = 889;

/*****************************/


IF (@debugBatchId IS NOT NULL)
BEGIN
	SELECT * FROM MaterialBatch WHERE Id = @debugBatchId;
END

BEGIN TRY

SET NOCOUNT ON;

UPDATE MaterialBatch SET BatchNumber = TRIM(BatchNumber);

-- We have to delete all invoicing data

DECLARE @iColId INT;
WHILE EXISTS(SELECT TOP 1 1 FROM InvoiceFormCollection)
BEGIN
	SET @iColId = (SELECT TOP 1 Id FROM InvoiceFormCollection);
	EXEC DeleteInvoiceFormCollection @iColId;
END


-- Record warehouse levels before the refactoring

DECLARE @imlvls TABLE (BatchId INT, UnitId INT, Available DECIMAL(19,5));
INSERT INTO @imlvls
EXEC CalculateBatchUsages 1;

DECLARE @lvls1 TABLE (MaterialId INT, BatchNumber NVARCHAR(100), Total DECIMAL(19,5), Available DECIMAL(19,5));

INSERT INTO @lvls1 (MaterialId, BatchNumber, Total, Available)
SELECT m.Id, mb.BatchNumber, SUM(mb.Volume), SUM(ISNULL(lvls.Available, -999999))
  FROM Material m
  JOIN MaterialBatch mb on mb.MaterialId = m.Id
  LEFT JOIN @imlvls lvls on lvls.BatchId = mb.Id
  WHERE m.NominalUnitId = 3
  GROUP BY m.Id, mb.BatchNumber;
  
-- 1. Create new materials
DECLARE @prefixMap TABLE (StepName NVARCHAR(100), Prefix NVARCHAR(100));

INSERT INTO @prefixMap SELECT N'Etiketování', N'NENAET' UNION 
                       SELECT N'Rozšroubování', N'NEROZSR';

IF EXISTS (SELECT MaterialId, COUNT(DISTINCT Id)
		  FROM MaterialProductionStep
		GROUP BY MaterialId
		HAVING COUNT(DISTINCT Id) > 1)
BEGIN
	THROW  51000, 'Material with multiple steps found', 1; 
END

IF EXISTS(SELECT * FROM MaterialProductionStep mps WHERE mps.Name NOT IN (SELECT StepName FROM @prefixMap))
BEGIN
	THROW  51000, 'Cannot map step name to prefix', 1; 
END

INSERT INTO hlpMaterialRefactoringLog (FinalMaterialId, MaterialProductionStepId)
SELECT m.Id, mps.Id
  FROM Material m
  JOIN MaterialProductionStep mps ON (m.Id = mps.MaterialId)
 WHERE m.Id NOT IN (SELECT FinalMaterialId FROM hlpMaterialRefactoringLog)
   AND ((@debugBatchId IS NULL) OR (m.Id = (SELECT sb.MaterialId FROM MaterialBatch sb WHERE sb.Id = @debugBatchId))) ;


 IF EXISTS(SELECT TOP 1 1 FROM hlpMaterialRefactoringLog lg
             JOIN Material m ON m.Id = lg.FinalMaterialId
			WHERE m.NominalUnitId <> 3)
BEGIN
	THROW  51000, 'Only "ks" unit allowed', 1; 
END


 PRINT 'precheck ok';

 WHILE EXISTS(SELECT TOP 1 1 FROM hlpMaterialRefactoringLog WHERE DerivedMaterialId IS NULL)
 BEGIN
	
	DECLARE @targetMaterialId INT = NULL;
	DECLARE @productionStepId INT = NULL;
	DECLARE @newMaterialId INT  = NULL;
	DECLARE @newRecipeId INT  = NULL;

	SELECT TOP 1 @targetMaterialId = FinalMaterialId, @productionStepId = MaterialProductionStepId FROM hlpMaterialRefactoringLog WHERE DerivedMaterialId IS NULL;

	DECLARE @dbgMatName NVARCHAR(100) = (SELECT Name FROM Material WHERE Id = @targetMaterialId);
	PRINT '>> Starting conversion of material = ' + @dbgMatName;

	INSERT INTO Material (Name,           NominalUnitId,   NominalAmount,   ProjectId, InventoryId, AutomaticBatches, RequiresInvoiceNr, RequiresPrice, RequiresSupplierReference)
	SELECT prefx.Prefix + ' ' + m.Name, m.NominalUnitId, m.NominalAmount, m.ProjectId,         2, m.AutomaticBatches, 0, 0, 0 
	  FROM Material m
	  JOIN MaterialProductionStep mps ON (m.Id = mps.MaterialId)
	  JOIN @prefixMap prefx ON (prefx.StepName = mps.Name)
	 WHERE m.Id = @targetMaterialId
	   AND mps.Id = @productionStepId;
	
	SET @newMaterialId = SCOPE_IDENTITY();

	SET @dbgMatName = (SELECT Name FROM Material WHERE Id = @newMaterialId);
	PRINT '>> Created new material ' + @dbgMatName;

	-- Create recipe
	INSERT INTO Recipe (RecipeName, ValidFrom, CreateUserId, ProducedMaterialId, RecipeProducedAmount, ProducedAmountUnitId, ProjectId, ProductionPricePerUnit)
	SELECT               mps.Name, '20190101', 2,             @targetMaterialId,     m.NominalAmount, m.NominalUnitId,     m.ProjectId, mps.PricePerUnit
	  FROM MaterialProductionStep mps 
	  JOIN Material               m   ON (mps.MaterialId = m.Id)
	 WHERE mps.Id = @productionStepId;
	
	SET @newRecipeId = SCOPE_IDENTITY();

	-- Create recipe components based on components of originating step:
	INSERT INTO RecipeComponent (MaterialId,   SortOrder, RecipeId, IsTransformationInput, UnitId, Amount)
	                     SELECT @targetMaterialId, sm.Id, @newRecipeId,                 0, sm.UnitId, sm.Amount
						   FROM MaterialProductionStepMaterial sm
						  WHERE sm.StepId = @productionStepId;
	
	-- add newly created material as main component of the recipe:
	INSERT INTO RecipeComponent (MaterialId,   SortOrder, RecipeId,   IsTransformationInput, UnitId, Amount)
	                     SELECT @newMaterialId,       0, @newRecipeId, 1, m.NominalUnitId, m.NominalAmount
						   FROM Material m
						  WHERE m.Id = @newMaterialId;
	
	UPDATE hlpMaterialRefactoringLog
	   SET DerivedMaterialId = @newMaterialId
	 WHERE FinalMaterialId = @targetMaterialId;


	 /***************** Process batches *******************************/
	DECLARE @batchesToConvert TABLE (BatchId INT);
	INSERT INTO @batchesToConvert 
	SELECT Id 
		FROM MaterialBatch
		WHERE MaterialId = @targetMaterialId
		  AND ((@debugBatchId IS NULL) OR (Id = @debugBatchId));

	WHILE EXISTS(SELECT TOP 1 1 FROM @batchesToConvert)
	BEGIN
		DECLARE @oldBatchId INT = (SELECT TOP 1 BatchId FROM @batchesToConvert);
		
		PRINT ' Converting batchId=' + STR(@oldBatchId);

		INSERT INTO MaterialBatch (MaterialId, Volume,     UnitId,   AuthorId,   BatchNumber,   Note,   Price,   ProjectId,   IsAvailable,   Produced,   Created,   InvoiceNr,   AllStepsDone, PriceConversionId, InvoiceVarSymbol,   SupplierId,   ProductionWorkPrice,    IsHiddenForAccounting) -- unfortunately we have no idea about the recipe; let's not forget about it
		SELECT                 @newMaterialId, b.Volume, b.UnitId, b.AuthorId, b.BatchNumber, b.Note, b.Price, b.ProjectId, b.IsAvailable, b.Produced, b.Created, b.InvoiceNr,           1, b.PriceConversionId, b.InvoiceVarSymbol, b.SupplierId, b.ProductionWorkPrice, b.IsHiddenForAccounting
		  FROM MaterialBatch b
		 WHERE b.Id = @oldBatchId;

		 DECLARE @newSourceBatchId INT = SCOPE_IDENTITY();

		 -- move components from old to the new batch
		 UPDATE MaterialBatchComposition
		    SET CompositionId = @newSourceBatchId
		  WHERE CompositionId = @oldBatchId;

         -- tjadadaaa, let's convert prod steps into normal production!
		 WHILE EXISTS(SELECT TOP 1 1 FROM BatchProductionStep WHERE BatchId = @oldBatchId)
		 BEGIN			
			DECLARE @batchProductionStepId INT;
			DECLARE @producedAmount DECIMAL(19,5);
			DECLARE @price DECIMAL(19,5);
			DECLARE @startDt DATETIME;
			DECLARE @endDt DATETIME;
			DECLARE @confirmUserId INT;
						
			SELECT TOP 1 
					@batchProductionStepId = theStep.Id,
					@producedAmount = theStep.ProducedAmount,
					@price = theStep.Price,
					@confirmUserId = theStep.ConfirmUserId,
					@startDt = theStep.ConfirmDt,
					@endDt =  ISNULL((SELECT TOP 1 ConfirmDt 
                    FROM BatchProductionStep nxt
				    WHERE nxt.BatchId = theStep.BatchId 
					  AND nxt.ConfirmDt > theStep.ConfirmDt 
					ORDER BY nxt.ConfirmDt), GETDATE())
			  FROM BatchProductionStep theStep  
			WHERE BatchId = @oldBatchId			
			ORDER BY theStep.ConfirmDt;

			PRINT '   Transforming production step: BatchProductionStepId=' + TRIM(STR(@batchProductionStepId)) + ' Amount=' + TRIM(STR(@producedAmount));

			-- create the batch instead of production step
			INSERT INTO MaterialBatch (MaterialId,          Volume, UnitId,     AuthorId,     BatchNumber, Note, ProjectId,     Created, IsAvailable, Produced, ProductionWorkPrice, Price, IsHiddenForAccounting, RecipeId)
			SELECT              @targetMaterialId, @producedAmount,    3, @confirmUserId, ob.BatchNumber,    '', ob.ProjectId, @startDt, 1,           @startDt,              @price,     0, ob.IsHiddenForAccounting, @newRecipeId
			  FROM MaterialBatch ob
			 WHERE ob.Id = @oldBatchId;
			
			DECLARE @stepBatchId INT = SCOPE_IDENTITY();

			PRINT '      Created new batch ID=' + TRIM(STR(@stepBatchId));

			INSERT INTO hlpBatchSplit (originalBatchId, partialBatchId, PartialStartDt)
			VALUES (@oldBatchId, @stepBatchId, @startDt);

			-- transform prod step material to components 
			INSERT INTO MaterialBatchComposition (CompositionId, ComponentId, UnitId, Volume)
			SELECT @stepBatchId, bsb.SourceBatchId, bsb.UnitId, bsb.UsedAmount
			  FROM BatchProuctionStepSourceBatch bsb
			 WHERE bsb.StepId = @batchProductionStepId;

			-- add the transformed batch
			INSERT INTO MaterialBatchComposition (CompositionId, ComponentId, UnitId, Volume)
			SELECT @stepBatchId, @newSourceBatchId, 3, @producedAmount;

			-- let's delete the batch step
			DELETE FROM BatchProuctionStepSourceBatch WHERE StepId = @batchProductionStepId;
			DELETE FROM BatchProductionStep WHERE Id = @batchProductionStepId;
		 END
        -----------
		DELETE FROM @batchesToConvert WHERE BatchId = @oldBatchId;
		PRINT 'Completed conversion of batchId=' + TRIM(STR(@oldBatchId));
	END	
 END

 -- delete step objects:
 DELETE FROM MaterialProductionStepMaterial;
 DELETE FROM MaterialProductionStep;
 --


DECLARE @realo TABLE (Id INT IDENTITY(1,1),
    objectName NVARCHAR(100),
	objectId BIGINT,
	maxBatchDt DATETIME,
	originalBatchId INT,
	amount DECIMAL(19,5));

 -- gather batch compositions to reallocate:
 INSERT INTO @realo (objectName, objectId, maxBatchDt, originalBatchId, amount)
 SELECT 'MaterialBatchComposition', mbc.Id, (SELECT sb.Created FROM MaterialBatch sb WHERE sb.Id = mbc.CompositionId), mbc.ComponentId, mbc.Volume
   FROM MaterialBatchComposition mbc
  WHERE mbc.ComponentId IN (SELECT originalBatchId FROM hlpBatchSplit);

 -- gather stock events to reallocate:
 INSERT INTO @realo (objectName, objectId, maxBatchDt, originalBatchId, amount)
 SELECT 'MaterialStockEvent', mse.Id, mse.EventDt, mse.BatchId, mse.Delta  
   FROM MaterialStockEvent mse
  WHERE mse.BatchId IN (SELECT originalBatchId FROM hlpBatchSplit);

 -- gather sale events to reallocate:
 INSERT INTO @realo (objectName, objectId, maxBatchDt, originalBatchId, amount)
   SELECT 'SaleEventAllocation', sea.Id, sea.AllocationDt, sea.BatchId, sea.AllocatedQuantity - sea.ReturnedQuantity
     FROM SaleEventAllocation sea
	WHERE sea.BatchId IN (SELECT originalBatchId FROM hlpBatchSplit);

 -- gather order items:
 INSERT INTO @realo (objectName, objectId, maxBatchDt, originalBatchId, amount)
  SELECT 'OrderItemMaterialBatch', omb.Id, omb.AssignmentDt, omb.MaterialBatchId, omb.Quantity 
    FROM OrderItemMaterialBatch omb
   WHERE omb.MaterialBatchId IN (SELECT originalBatchId FROM hlpBatchSplit);

IF (@debugBatchId IS NOT NULL)
BEGIN
	PRINT 'WARNING - DEBUG batch filter used';
	delete from @realo where originalBatchId <> @debugBatchId;
END

DECLARE @rlcnt INT = (SELECT COUNT(*) FROM @realo);
PRINT 'Processing reallocations COUNT=' + TRIM(STR(@rlcnt));
-- process reallocations:

DECLARE @failedAllocationsCount INT = 0;

WHILE EXISTS (SELECT TOP 1 1 FROM @realo)
BEGIN
    DECLARE @realoid INT;
	DECLARE @objectName NVARCHAR(100);
	DECLARE @objectId BIGINT;
	DECLARE @maxBatchDt DATETIME;
	DECLARE @originalBatchId INT;
	DECLARE @amount DECIMAL(19,5);

	SELECT TOP 1 
	       @realoid = id,
	       @objectName = objectName,
	       @objectId = objectId,
		   @maxBatchDt = maxBatchDt,
		   @originalBatchId = originalBatchId,
		   @amount = amount
       FROM @realo
	ORDER BY id;

	BEGIN TRY
		EXEC sp_reallocate @objectName, @objectId, @maxBatchDt, @originalBatchId, @amount;
	END TRY
	BEGIN CATCH
		SET @failedAllocationsCount = @failedAllocationsCount + 1;
	END CATCH

	DELETE FROM @realo WHERE id = @realoid;
END

PRINT 'allocations done';
IF (@failedAllocationsCount > 0)
BEGIN
    DECLARE @m NVARCHAR(100) =  'allocations failed = ' + TRIM(STR(@failedAllocationsCount));
	PRINT @m;
	THROW  51000, @m, 1; 
END

 -- check that any amount didn't change

DELETE FROM @imlvls;
INSERT INTO @imlvls
EXEC CalculateBatchUsages 1;

DECLARE @lvls2 TABLE (MaterialId INT, BatchNumber NVARCHAR(100), Total DECIMAL(19,5), Available DECIMAL(19,5));

INSERT INTO @lvls2 (MaterialId, BatchNumber, Total, Available)
SELECT m.Id, mb.BatchNumber, SUM(mb.Volume), SUM(ISNULL(lvls.Available, -999999))
  FROM Material m
  JOIN MaterialBatch mb on mb.MaterialId = m.Id
  LEFT JOIN @imlvls lvls on lvls.BatchId = mb.Id
  WHERE m.NominalUnitId = 3
  GROUP BY m.Id, mb.BatchNumber;

IF ((SELECT COUNT(*) FROM @lvls1) <> (SELECT COUNT(*) FROM @lvls2))
BEGIN
	THROW  51000, 'Different count of records in comparison check tables', 1; 
END

IF EXISTS(SELECT TOP 1 1 FROM (
		SELECT l1.MaterialId, l1.BatchNumber, l1.Total - l2.Total TotalDiff, l1.Available - l2.Available AvailDiff
			 FROM @lvls1 l1
		LEFT JOIN @lvls2 l2 ON (l1.MaterialId = l2.MaterialId AND l1.BatchNumber = l2.BatchNumber)) x
	    WHERE x.TotalDiff <> 0 OR x.AvailDiff <> 0)
BEGIN
	THROW  51000, 'Levels comparison check failed!', 1; 
END



END TRY
BEGIN CATCH
	ROLLBACK; 
	PRINT ERROR_MESSAGE();
END CATCH

PRINT '!!! Don`t forget to backup the database before commit!';
ROLLBACK;




 