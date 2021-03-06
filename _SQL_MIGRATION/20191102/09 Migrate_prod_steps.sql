
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
	(id INT IDENTITY(1,1),
	 objectName NVARCHAR(100),
	 objectId BIGINT,
	 batchId INT,
	 amount DECIMAL(19, 5));

GO
IF EXISTS(SELECT * FROM sys.procedures WHERe name = 'sp_reallocate')
BEGIN
	DROP PROCEDURE sp_reallocate;
END

GO

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.CONSTRAINT_TABLE_USAGE t1
			  JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE t2 ON t1.TABLE_NAME=t2.TABLE_NAME
              WHERE OBJECTPROPERTY(OBJECT_ID(t1.CONSTRAINT_NAME),'IsUniqueCnst')=1
              AND t1.TABLE_NAME='RecipeComponent')
BEGIN
	ALTER TABLE RecipeComponent ADD CONSTRAINT UC_component UNIQUE (RecipeId, MaterialId);
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

			INSERT INTO hlpReallocations (objectName, objectId, batchId, amount)
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

DECLARE @debugBatchId INT;-- = 807;

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

-- delete overallocated orders
delete from OrderItemMaterialBatch where id in (
select distinct oimb.Id
  from PurchaseOrder po
  join OrderItem oi on oi.PurchaseOrderId = po.id
  left join OrderItem ki on ki.KitParentId = oi.Id
  join OrderItemMaterialBatch oimb ON (oimb.OrderItemId = ISNULL(ki.Id, oi.Id))
where po.OrderStatusId IN (6,7,8));
  
-- 1. Create new materials
DECLARE @prefixMap TABLE (StepName NVARCHAR(100), Prefix NVARCHAR(100));

INSERT INTO @prefixMap SELECT N'Etiketov�n�', N'NENAET' UNION 
                       SELECT N'Roz�roubov�n�', N'NEROZSR';

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
   AND ((@debugBatchId IS NULL) OR (m.Id = (SELECT sb.MaterialId FROM MaterialBatch sb WHERE sb.Id = @debugBatchId)));


 IF EXISTS(SELECT TOP 1 1 FROM hlpMaterialRefactoringLog lg
             JOIN Material m ON m.Id = lg.FinalMaterialId
			WHERE m.NominalUnitId <> 3)
BEGIN
	THROW  51000, 'Only "ks" unit allowed', 1; 
END


 PRINT 'precheck ok';

 -- Record warehouse levels before the refactoring -------------------

 DECLARE @lvl1 TABLE (MaterialId INT, Material NVARCHAR(200), Vstup DECIMAL(19, 5), Vyrobeno DECIMAL(19,5));
 INSERT INTO @lvl1
 SELECT m.Id MaterialId, 
       m.Name Material, 
	   totalVolumes.total Vstup, 
	   CASE WHEN mps.Id IS NULL THEN totalVolumes.total ELSE ISNULL(finalProduced.total, 0) END Vyrobeno
  FROM Material m
  LEFT JOIN (SELECT zmb.MaterialId, SUM(zmb.Volume) total
               FROM MaterialBatch zmb
			 GROUP BY zmb.MaterialId) totalVolumes ON (totalVolumes.MaterialId = m.Id)
			 
  LEFT JOIN (SELECT xmb.MaterialId, SUM(bps.ProducedAmount) total 
               FROM BatchProductionStep bps
			   JOIN MaterialBatch xmb ON (bps.BatchId = xmb.Id)
              GROUP BY xmb.MaterialId) finalProduced ON (finalProduced.MaterialId = m.Id)
  LEFT JOIN MaterialProductionStep mps ON (mps.MaterialId = m.Id)
WHERE m.NominalUnitId = 3;

----------------------------------------------------------------------


 WHILE EXISTS(SELECT TOP 1 1 FROM hlpMaterialRefactoringLog WHERE DerivedMaterialId IS NULL)
 BEGIN
	
	DECLARE @targetMaterialId INT = NULL;
	DECLARE @productionStepId INT = NULL;
	DECLARE @newMaterialId INT  = NULL;
	DECLARE @newRecipeId INT  = NULL;

	SELECT TOP 1 @targetMaterialId = FinalMaterialId, @productionStepId = MaterialProductionStepId FROM hlpMaterialRefactoringLog WHERE DerivedMaterialId IS NULL;

	DECLARE @dbgMatName NVARCHAR(100) = (SELECT Name FROM Material WHERE Id = @targetMaterialId);
	PRINT '>> Starting conversion of material = ' + @dbgMatName;

	INSERT INTO Material (Name,           NominalUnitId,   NominalAmount,   ProjectId,   InventoryId, AutomaticBatches, RequiresInvoiceNr, RequiresPrice, RequiresSupplierReference)
	SELECT prefx.Prefix + ' ' + m.Name, m.NominalUnitId, m.NominalAmount, m.ProjectId, CASE WHEN m.InventoryId < 3 THEN m.InventoryId ELSE 2 END , m.AutomaticBatches, 0, 0, 0 
	  FROM Material m
	  JOIN MaterialProductionStep mps ON (m.Id = mps.MaterialId)
	  JOIN @prefixMap prefx ON (prefx.StepName = mps.Name)
	 WHERE m.Id = @targetMaterialId
	   AND mps.Id = @productionStepId;
	
	SET @newMaterialId = SCOPE_IDENTITY();

	SET @dbgMatName = (SELECT Name FROM Material WHERE Id = @newMaterialId);
	PRINT '>> Created new material ' + @dbgMatName;
	 
	-- Create recipe from production step
	INSERT INTO Recipe (RecipeName, ValidFrom, CreateUserId, ProducedMaterialId, RecipeProducedAmount, ProducedAmountUnitId, ProjectId, ProductionPricePerUnit)
	SELECT               mps.Name, '20190101', 2,             @targetMaterialId,     m.NominalAmount, m.NominalUnitId,     m.ProjectId, mps.PricePerUnit
	  FROM MaterialProductionStep mps 
	  JOIN Material m ON (mps.MaterialId = m.Id)	   
	 WHERE mps.Id = @productionStepId;
	
	SET @newRecipeId = SCOPE_IDENTITY();

	-- Create recipe components based on components of originating step:
	INSERT INTO RecipeComponent (MaterialId,   SortOrder, RecipeId, IsTransformationInput, UnitId, Amount)
	                     SELECT sm.MaterialId, sm.Id, @newRecipeId,                 0, sm.UnitId, sm.Amount
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

	 -- Create recipe from material components
     IF EXISTS(SELECT TOP 1 1 FROM MaterialComposition mc WHERE mc.CompositionId = @targetMaterialId)
	 BEGIN	 
		INSERT INTO Recipe (RecipeName, ValidFrom, CreateUserId, ProducedMaterialId, RecipeProducedAmount, ProducedAmountUnitId, ProjectId, ProductionPricePerUnit)
		SELECT               N'V�roba' , '20190101', 2,             @newMaterialId,  m.NominalAmount, m.NominalUnitId,     m.ProjectId, null
		  FROM Material               m  
		 WHERE m.Id = @newMaterialId;
	
		DECLARE @migratedRecipeId INT = SCOPE_IDENTITY();

		INSERT INTO RecipeComponent (MaterialId, SortOrder, RecipeId, IsTransformationInput, UnitId, Amount)
		SELECT mc.ComponentId, mc.Id, @migratedRecipeId, 0, mc.UnitId, mc.Amount 
		  FROM MaterialComposition mc
		 WHERE mc.CompositionId = @targetMaterialId;
		
		DELETE FROM MaterialComposition WHERE CompositionId = @targetMaterialId;
	 END


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

		IF NOT EXISTS(SELECT TOP 1 1 FROM BatchProductionStep bps WHERE bps.BatchId = @oldBatchId)
		BEGIN
			PRINT '  Batch doesn`t have any prod steps - just updating to be of the new material';

			UPDATE MaterialBatch
			   SET MaterialId = @newMaterialId
			 WHERE Id = @oldBatchId;

		END
		ELSE
		BEGIN

			INSERT INTO MaterialBatch (MaterialId, Volume,     UnitId,   AuthorId,   BatchNumber,   Note,   Price,   ProjectId,   IsAvailable,   Produced,   Created,   InvoiceNr,   AllStepsDone, PriceConversionId, InvoiceVarSymbol,   SupplierId,   ProductionWorkPrice,    IsHiddenForAccounting) -- unfortunately we have no idea about the recipe; let's not forget about it
			SELECT                 @newMaterialId, b.Volume, b.UnitId, b.AuthorId, b.BatchNumber, b.Note, b.Price, b.ProjectId, b.IsAvailable, b.Produced, b.Created, b.InvoiceNr,           1, b.PriceConversionId, b.InvoiceVarSymbol, b.SupplierId, b.ProductionWorkPrice, b.IsHiddenForAccounting
			  FROM MaterialBatch b
			 WHERE b.Id = @oldBatchId;

			 DECLARE @newSourceBatchId INT = SCOPE_IDENTITY();
		 
			 DECLARE @intermMatName NVARCHAR(200) = (SELECT TOP 1 Name FROM Material WHERE Id = @newMaterialId);
			 PRINT '  Batch created Material= ' + @intermMatName + ' BatchId=' + TRIM(STR(@newSourceBatchId));
		 		 
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
		END
        -----------
		DELETE FROM @batchesToConvert WHERE BatchId = @oldBatchId;
				
		PRINT 'Completed conversion of batchId=' + TRIM(STR(@oldBatchId));
	END	
 END

 -- delete steps:
 IF (@debugBatchId IS NULL)
 BEGIN 
	DELETE FROM MaterialProductionStepMaterial;
	DELETE FROM MaterialProductionStep;
 END
 --

 -- Migrate components to recipes
 WHILE EXISTS (SELECT TOP 1 1 FROM MaterialComposition)
 BEGIN
 	DECLARE @coCompositionId INT = (SELECT TOP 1 CompositionId FROM MaterialComposition);

	INSERT INTO Recipe (RecipeName, ValidFrom, CreateUserId, ProducedMaterialId, RecipeProducedAmount, ProducedAmountUnitId, ProjectId, ProductionPricePerUnit)
		SELECT               N'V�roba' , '20190101', 2,             @coCompositionId,  m.NominalAmount, m.NominalUnitId,     m.ProjectId, null
		  FROM Material               m  
		 WHERE m.Id = @coCompositionId;
	
	DECLARE @coMigratedRecipeId INT = SCOPE_IDENTITY();

	INSERT INTO RecipeComponent (MaterialId, SortOrder, RecipeId, IsTransformationInput, UnitId, Amount)
	SELECT mc.ComponentId, mc.Id, @coMigratedRecipeId, 0, mc.UnitId, mc.Amount 
		FROM MaterialComposition mc
		WHERE mc.CompositionId = @coCompositionId;
		
	DELETE FROM MaterialComposition WHERE CompositionId = @coCompositionId;
 END



 -- Move materials with recipes to manufactured inventory
UPDATE Material SET InventoryId = 2
WHERE InventoryId = 1
AND Id IN 
(
	SELECT ProducedMaterialId 
	  FROM Recipe
);


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
ELSE
BEGIN
	PRINT 'Allocations OK!';
END

-------------------------------------------------------------------------------------------------------------------
DECLARE @upsertSource TABLE (objectName NVARCHAR(200) NOT NULL, objectId BIGINT NULL, templateId BIGINT NOT NULL, batchId INT NOT NULL, amount DECIMAL(19, 5));
INSERT INTO @upsertSource
SELECT r.objectName, 
       case when up.minid = r.id then r.objectId else null end, 
	   r.objectId,
	   r.batchId,
	   r.amount
  FROM hlpReallocations r
  JOIN (SELECT sr.objectName, sr.objectId, MIN(sr.id) as minid
         FROM hlpReallocations sr
		GROUP BY sr.objectName, sr.objectId) as up ON (up.objectName = r.objectName AND up.objectId = r.objectId);

MERGE MaterialStockEvent as tgt
USING (SELECT s.objectId as _objectId,
              s.batchId  as _batchId,
			  s.amount   as _amount,
			  template.* 
         FROM @upsertSource s 
		 JOIN MaterialStockEvent template ON (s.templateId = template.Id)
	   WHERE s.objectName = 'MaterialStockEvent') as src ON (tgt.Id = src._objectId)
WHEN MATCHED THEN
	UPDATE SET tgt.BatchId = src._batchId,	           		
	           tgt.Delta = src._amount
WHEN NOT MATCHED THEN
	INSERT (BatchId, UnitId, Delta, ProjectId, TypeId, Note, UserId, EventGroupingKey, EventDt, SourcePurchaseOrderId)
	VALUES (src._batchId, src.UnitId, src._amount, src.ProjectId, src.TypeId, src.Note, src.UserId, src.EventGroupingKey, src.EventDt, src.SourcePurchaseOrderId);


MERGE OrderItemMaterialBatch as tgt
USING (SELECT s.objectId as _objectId,
              s.batchId  as _batchId,
			  s.amount   as _amount,
			  template.* 
         FROM @upsertSource s 
		 JOIN OrderItemMaterialBatch template ON (s.templateId = template.Id)
	   WHERE s.objectName = 'OrderItemMaterialBatch') as src ON (tgt.Id = src._objectId)
WHEN MATCHED THEN
	UPDATE SET tgt.MaterialBatchId = src._batchId,	           		
	           tgt.Quantity = src._amount
WHEN NOT MATCHED THEN
	INSERT (OrderItemId,      MaterialBatchId, Quantity, UserId, AssignmentDt)
	VALUES (src.OrderItemId,  src._batchId,    src._amount, src.UserId, src.AssignmentDt);


MERGE MaterialBatchComposition as tgt
USING (SELECT s.objectId as _objectId,
              s.batchId  as _batchId,
			  s.amount   as _amount,
			  template.* 
         FROM @upsertSource s 
		 JOIN MaterialBatchComposition template ON (s.templateId = template.Id)
	   WHERE s.objectName = 'MaterialBatchComposition') as src ON (tgt.Id = src._objectId)
WHEN MATCHED THEN
	UPDATE SET tgt.ComponentId = src._batchId,	           		
	           tgt.Volume = src._amount
WHEN NOT MATCHED THEN
	INSERT (CompositionId,       ComponentId, UnitId,      Volume)
	VALUES (src.CompositionId,  src._batchId, src.UnitId,  src._amount);
	

MERGE SaleEventAllocation as tgt
USING (SELECT s.objectId as _objectId,
              s.batchId  as _batchId,
			  s.amount   as _amount,
			  template.* 
         FROM @upsertSource s 
		 JOIN SaleEventAllocation template ON (s.templateId = template.Id)
	   WHERE s.objectName = 'SaleEventAllocation') as src ON (tgt.Id = src._objectId)
WHEN MATCHED THEN
	UPDATE SET tgt.BatchId = src._batchId,	           		
	           tgt.AllocatedQuantity = src._amount,
			   tgt.ReturnedQuantity = 0
WHEN NOT MATCHED THEN
	INSERT (SaleEventId, BatchId, AllocatedQuantity, ReturnedQuantity, UnitId, AllocationDt, ReturnDt, AllocationUserId, ReturnUserId)
	VALUES (src.SaleEventId, src._batchId, src._amount, 0, src.UnitId, src.AllocationDt, src.ReturnDt, src.AllocationUserId, src.ReturnUserId);

-- delete reassinged batches

DELETE FROM MaterialBatch
WHERE Id IN (SELECT s.originalBatchId FROM hlpBatchSplit s);

 -- Check amounts didnt change  -------------------

DECLARE @lvl2 TABLE (MaterialId INT, Material NVARCHAR(200), Vstup DECIMAL(19, 5), Vyrobeno DECIMAL(19,5), Orimat NVARCHAR(200));
INSERT INTO @lvl2
SELECT m.Id MaterialId, 
       m.Name Material, 
	   CASE WHEN orimat.Name IS NULL THEN ISNULL(totalVolumes.total, 0) ELSE ISNULL(orimat.volume, 0) END Vstup,
	   ISNULL(totalVolumes.total, 0) Vyrobeno,
	   orimat.Name	   
  FROM Material m
  LEFT JOIN (SELECT zmb.MaterialId, SUM(zmb.Volume) total
               FROM MaterialBatch zmb
			 GROUP BY zmb.MaterialId) totalVolumes ON (totalVolumes.MaterialId = m.Id)
   LEFT JOIN (
		SELECT xm.Name, SUM(xb.Volume) volume
		  FROM MaterialBatch xb
		  JOIN Material xm ON (xm.Id = xb.MaterialId)
		 WHERE xm.NominalUnitId = 3
		 GROUP BY xm.Name) orimat ON (orimat.Name <> m.Name AND orimat.Name LIKE '%' + m.Name)
WHERE m.NominalUnitId = 3
  AND m.Id NOT IN (SELECT rlg.DerivedMaterialId FROM hlpMaterialRefactoringLog rlg);
  --AND EXISTS(SELECT TOP 1 1 FROM RecipeComponent rc WHERE rc.MaterialId = m.Id);



SELECT allMat.MaterialId, masterMat.Name 'Master', a.*, b.*
FROM ((SELECT DISTINCT a.MaterialId FROM @lvl1 a) UNION (SELECT DISTINCT b.MaterialId FROM @lvl2 b)) allMat 
LEFT JOIN Material masterMat ON (allMat.MaterialId = masterMat.Id)
LEFT JOIN @lvl1 a ON (allMat.MaterialId = a.MaterialId)
LEFT JOIN @lvl2 b ON (allMat.MaterialId = b.MaterialId);

--SELECT m.Name, xm.Name
--  FROM Material m
--  LEFT JOIN Material xm ON (xm.Id <> m.Id AND xm.Name like '%'+m.Name)
-- WHERE m.NominalUnitId = 3 AND xm.NominalUnitId = 3;


IF EXISTS(SELECT TOP 1 1 
            FROM @lvl1 a 
			LEFT JOIN @lvl2 b ON (a.MaterialId = b.MaterialId)
			WHERE (ISNULL(a.Vstup, 0) - ISNULL(b.Vstup, 0)) <> 0
			   OR (ISNULL(a.Vyrobeno, 0) - ISNULL(b.Vyrobeno, 0)) <> 0)
BEGIN
	SELECT *
            FROM @lvl1 a 
			LEFT JOIN @lvl2 b ON (a.MaterialId = b.MaterialId)
			WHERE (ISNULL(a.Vstup, 0) - ISNULL(b.Vstup, 0)) <> 0
			   OR (ISNULL(a.Vyrobeno, 0) - ISNULL(b.Vyrobeno, 0)) <> 0;

    THROW 50001, 'Levels dont match', 1;
END

IF EXISTS(SELECT * FROM @lvl1 a WHERE NOT EXISTS(SELECT TOP 1 1 FROM @lvl2 b WHERE a.MaterialId = b.MaterialId))
BEGIN
	THROW 50001, 'Levels dont match - more records before refactoring', 1;
END

IF EXISTS(SELECT * FROM @lvl2 a WHERE NOT EXISTS(SELECT TOP 1 1 FROM @lvl1 b WHERE a.MaterialId = b.MaterialId))
BEGIN
	THROW 50001, 'Levels dont match - more records after refactoring', 1;
END


----------------------------------------------------------------------


END TRY
BEGIN CATCH
	ROLLBACK; 
	PRINT ERROR_MESSAGE();
END CATCH

--COMMIT

--ROLLBACK


/*

SELECT * FROM MaterialBatch where BatchNumber = '19312'

select * from material where name like '%Deodorant Neparf�movan� 15g'

*/

 