--EXEC sp_reduceBatchAmount N'Inventura 2019', '20191219', N'XL Pleťový krém Smyslná a věrná sama sobě 60ml', N'19335', N'14', N'ks', N'14';

ALTER PROCEDURE sp_reduceBatchAmount (
	@reason NVARCHAR(200),
	@date DATETIME,
	@materialName NVARCHAR(200),
	@batchNumber NVARCHAR(200),
	@currentAmountStr NVARCHAR(10),
	@unitSymbol NVARCHAR(10),
	@newAmountStr NVARCHAR(10)
)
AS
BEGIN
	SET NOCOUNT ON;
	
	PRINT 'Starting ' + @batchNumber + ' ' + @materialName;
			
	DECLARE @materialId INT = (SELECT TOP 1 Id FROM Material WHERE Name = @materialName);
	DECLARE @newAmount DECIMAL(19, 5) = CAST(@newAmountStr as DECIMAL(19, 5));
	DECLARE @unitId INT = (SELECT TOP 1 Id FROM MaterialUnit WHERE Symbol = @unitSymbol);
		
	IF (@materialId IS NULL)
	BEGIN
		PRINT @materialName;
		THROW 51000, 'Material', 1;  
	END
	
	DECLARE @resolution TABLE (BatchId INT, UnitId INT, Available DECIMAL(19, 5));
	INSERT INTO @resolution
	EXEC CalculateBatchUsages 1, null, @materialId;	

	DELETE FROM @resolution WHERE BatchId NOT IN (SELECT Id FROM MaterialBatch WHERE MAterialId = @materialId AND BatchNumber = @batchNumber);

	DECLARE @currentAvail DECIMAL(19, 5) = (SELECT SUM(dbo.ConvertToUnit(1, Available, UnitId, @unitId)) FROM @resolution);

	DECLARE @toThrow DECIMAL(19, 5) = @currentAvail - @newAmount;

	IF (@toThrow = 0)
	BEGIN
		PRINT '   Pozadovane mnozstvi je aktualni stav';
		RETURN;
	END

	IF (@toThrow < 0)
	BEGIN
		PRINT '   !!! Vysledne mnozstvi by bylo zaporne';
		RETURN;
	END

	BEGIN TRAN;
	BEGIN TRY;

		PRINT '   * zacinam';

		DECLARE @gk NVARCHAR(32) = RIGHT(CAST(NEWID() AS VARCHAR(50)), 32);

		WHILE EXISTS(SELECT TOP 1 1 FROM @resolution)
		BEGIN
			DECLARE @aloTargetId INT;
			DECLARE @available DECIMAL(19, 5);

			SET @aloTargetId = NULL; SET @available = NULL;

			SELECT TOP 1 @aloTargetId = BatchId, @available = dbo.ConvertToUnit(1, Available, UnitId, @unitId) FROM @resolution ORDER BY BatchId;

			DECLARE @removeHere DECIMAL(19, 5) = @toThrow;
			IF (@toThrow > @available)
			BEGIN
				SET @removeHere = @available;			
			END

		
			INSERT INTO MaterialStockEvent (BatchId, UnitId, Delta, ProjectId, TypeId, Note, UserId, EventGroupingKey, EventDt)
			VALUES (@aloTargetId, @unitId, @removeHere, 1, 1, @reason, 2, @gk, @date);
		
			SET @toThrow = @toThrow - @removeHere;

			DELETE FROM @resolution WHERE @toThrow = 0 OR BatchId = (SELECT TOP 1 BatchId FROM @resolution ORDER BY BatchId);
		END

		IF (@toThrow <> 0)
		BEGIN
			THROW 51000, 'Vysledne mnozstvi neni 0', 1;
		END
	
		COMMIT TRAN;
		PRINT 'HOTOVO';	

	END TRY
	BEGIN CATCH
		ROLLBACK TRAN;
		PRINT ERROR_MESSAGE();
	END CATCH

END

