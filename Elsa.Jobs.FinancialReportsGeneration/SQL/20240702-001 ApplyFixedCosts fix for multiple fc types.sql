IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'ApplyFixedCosts')
	DROP PROCEDURE ApplyFixedCosts;

GO

CREATE PROCEDURE [dbo].[ApplyFixedCosts] (
	@projectId INT,
	@userId INT,
	@year INT,
	@month INT,
	@overwrite BIT)
AS
BEGIN

	IF (@overwrite = 0 AND EXISTS(SELECT TOP 1 1 FROM FixedCostMonthCalculation fmc WHERE fmc.ProjectId = @projectId AND fmc.Month = @month AND fmc.Year = @year))
	BEGIN
		RETURN;
	END

	BEGIN TRY
		BEGIN TRAN;
		
		DECLARE @batches TABLE (id INT);
		INSERT INTO @batches
		SELECT mb.Id
		  FROM MaterialBatch mb

		 WHERE  -- manufactured
				EXISTS(SELECT TOP 1 1 
						FROM MaterialBatchComposition mbc
					   WHERE mbc.CompositionId = mb.Id)
			   -- not manufactured from manufactured
		   AND NOT EXISTS(SELECT TOP 1 1
							FROM MaterialBatchComposition mbc
							JOIN MaterialBatchComposition submbc ON (mbc.ComponentId = submbc.CompositionId)
						   WHERE mbc.CompositionId = mb.Id)
		  AND YEAR(mb.Created) = @year
		  AND MONTH(mb.Created) = @month
		  AND mb.ProjectId = @projectId;


		DECLARE @toInvalidate TABLE (id INT);
		INSERT INTO @toInvalidate 
		  SELECT Id
			FROM @batches
		  UNION (
			SELECT fbc.BatchId as Id
			  FROM FixedCostBatchComponent fbc
			  JOIN FixedCostMonthCalculation fcc ON (fbc.CalculationId = fcc.Id)
			 WHERE fcc.ProjectId = @projectId
			   AND fcc.Month = @month
			   AND fcc.Year = @year
		  ); 

		WHILE (1=1)
		BEGIN
			DECLARE @batchId INT = (SELECT TOP 1 id FROM @toInvalidate);
			IF (@batchId IS NULL) 
				BREAK;

			DELETE FROM FixedCostBatchComponent WHERE BatchId = @batchId;

			EXEC OnBatchChanged @batchId;

			DELETE FROM @toInvalidate WHERE id = @batchId;
		END
		
		DELETE FROM FixedCostMonthCalculation
		 WHERE ProjectId = @projectId
		   AND [Year] = @year
		   AND [Month] = @month;

		-- check there is no missing fixed cost value
		DECLARE @missingFcValues NVARCHAR(1000);
		SELECT @missingFcValues = N'Nebyly zadány tyto hodnoty nepřímých nákladů: ' + STRING_AGG(fct.Name, N', ')
		  FROM FixedCostType fct  
		 WHERE fct.ProjectId = @projectId
		   AND NOT EXISTS(SELECT TOP 1 1 
							FROM FixedCostValue fcv 
						   WHERE fcv.FixedCostTypeId = fct.Id
							 AND fcv.Month = @month
							 AND fcv.Year = @year);

		IF @missingFcValues IS NOT NULL
		BEGIN	
			THROW 50001, @missingFcValues, 1;
		END
		;

		DECLARE @toDistribute DECIMAL(19, 5) = (
			SELECT SUM(fcv.Value / 100 * CAST(fct.PercentToDistributeAmongProducts AS DECIMAL(19, 5)))
			  FROM FixedCostValue fcv
			  JOIN FixedCostType fct ON (fcv.FixedCostTypeId = fct.Id)
			 WHERE fcv.Month = @month
			   AND fcv.Year = @year
			   AND fcv.ProjectId = @projectId
		);

		IF (@toDistribute IS NULL)
		BEGIN
			THROW 50001, N'Nepřímé náklady nebyly zadány', 1;
		END

		DECLARE @totalDistrib DECIMAL(19, 5) = ISNULL((
			SELECT SUM(dbo.GetBatchPrice(mb.Id, @projectId, mb.Volume, mb.UnitId))
			  FROM MaterialBatch mb
			  JOIN @batches b ON (mb.Id = b.id)), 0);		  	

		DECLARE @dist DECIMAL(19, 5) = 0;

		IF (@totalDistrib > 0)
		BEGIN
			SET @dist = @toDistribute / @totalDistrib;
		END

		INSERT INTO FixedCostMonthCalculation (Year, Month, ValueToDistribute, DistributeToTotal, DistributionDiv, ProjectId, CalcUserId, CalcDt)
		VALUES (@year, @month, @toDistribute, @totalDistrib, @dist, @projectId, @userId, GETDATE());

		DECLARE @calculationId INT = SCOPE_IDENTITY();

		INSERT INTO FixedCostBatchComponent (CalculationId, BatchId, Value, Multiplier)
		SELECT @calculationId, mb.Id, dbo.GetBatchPrice(mb.Id, @projectId, mb.Volume, mb.UnitId) * @toDistribute / @totalDistrib, dbo.GetBatchPrice(mb.Id, @projectId, mb.Volume, mb.UnitId) 
		  FROM MaterialBatch mb
		  JOIN @batches b ON (mb.Id = b.id);

		WHILE(1=1)
		BEGIN
			SET @batchId = (SELECT TOP 1 Id FROM @batches);
			IF (@batchId IS NULL)
				BREAK;

			EXEC OnBatchChanged @batchId;

			DELETE FROM @batches WHERE id = @batchId;
		END

		COMMIT TRAN;

	END TRY
	BEGIN CATCH
		ROLLBACK TRAN;
		THROW;
	END CATCH

END
GO




