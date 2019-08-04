ALTER PROCEDURE sp_importBatch
(@authorId     INT,
 @materialName NVARCHAR(300),
 @quantityStr  NVARCHAR(300),
 @supplierName NVARCHAR(300),
 @invoiceNr    NVARCHAR(100),
 @varSymb      NVARCHAR(100),
 @dummy        NVARCHAR(100),
 @czkPriceStr  NVARCHAR(100),
 @batchNr      NVARCHAR(100),
 @date         DATETIME,
 @note         NVARCHAR(500) = NULL)
AS
BEGIN    
	BEGIN TRY
		BEGIN TRAN;

		DECLARE @quantity DECIMAL(19,4);
		DECLARE @czkPrice DECIMAL(19,4);

		SET @quantity = PARSE(@quantityStr AS DECIMAL(19,4));		
		SET @czkPrice = PARSE(@czkPriceStr AS DECIMAL(19,4));

		DECLARE @projectId INT;
		SET @projectId = (SELECT TOP 1 ProjectId FROM [User] WHERE Id = @authorId);
				
		PRINT 'Importuji ' + @materialName + ' ' + @batchNr;
		DECLARE @materialId INT;

		PRINT '  qty vstup:' + @quantityStr;
		PRINT @quantity;

		SET @materialId = (SELECT TOP 1 Id FROM Material WHERE Name = @materialName);
		IF (@materialId IS NULL)
		BEGIN
			THROW 51000, 'Material nenalezen', 1;			
		END
		PRINT '  Material ok';

		DECLARE @supplierId INT;
		IF (LEN(@supplierName) > 1)
		BEGIN
			SET @supplierId = (SELECT TOP 1 Id FROM Supplier WHERE Name = @supplierName);
			IF (@supplierId IS NULL)
			BEGIN
				THROW 51000, 'Dodavatel nenalezen', 1;			
			END
			PRINT '  Dodavatel ok';
		END
		ELSE
		BEGIN
			PRINT ' Bez dodavatele';
		END
	

		IF EXISTS(SELECT TOP 1 1 FROM MaterialBatch WHERE MaterialId = @materialId AND BatchNumber = @batchNr)
		BEGIN
			THROW 51000, 'Duplicitni cislo sarze', 1;			
		END
		PRINT ' Cislo sarze ok';

		EXEC sp_fakeBatch @materialId, @quantity, @authorId;

		DECLARE @batchId INT;
		SET @batchId = (SELECT MAX(Id) FROM MaterialBatch WHERE MaterialId = @materialId);

		UPDATE MaterialBatch
		   SET Price = @czkPrice,
		       Volume = @quantity,
		       BatchNumber = @batchNr,
			   Note = ISNULL(@note, 'aut. import - prodej sro'),
			   Created = @date,
			   IsAvailable = 1,
			   AllStepsDone = 1,
			   InvoiceNr = @invoiceNr,
			   InvoiceVarSymbol = @varSymb,
			   SupplierId = @supplierId
		  WHERE Id = @batchId;


		
		PRINT 'HOTOVO';
		PRINT '';

		COMMIT;
		
	END TRY
	BEGIN CATCH
		ROLLBACK;
		THROW;
	END CATCH
END


/*

@authorId     INT,
 @materialName NVARCHAR(300),
 @quantityStr  NVARCHAR(300),
 @supplierName NVARCHAR(300),
 @invoiceNr    NVARCHAR(100),
 @varSymb      NVARCHAR(100),
 @dummy        NVARCHAR(100),
 @czkPriceStr  NVARCHAR(100),
 @batchNr      NVARCHAR(100),
 @date         DATETIME,
 @note         NVARCHAR(500),
 @sourceBatchesFilter NVARCHAR(2000) = null

  exec sp_importBatch 2, 
  'XL Pleťový krém Konopný olej - "Anti-pupínek" 60ml', 
  '10.56', 
  'SOLIA, spol. s r.o.', 
  'invnum', 
  'varsym', 
  ''
  ,'123.65', 
  'basdftgyh210', 
  '20190801', 
  'poznblblbl'; 
  
  BEGIN TRAN;
  EXEC sp_importBatch 2, N'Tuba deo 30g (tělo)', N'1290', N'', N'2019-0027', N'20190027', N'6.3', N'8127.00', N'', '20190731';
  EXEC sp_importBatch 2, N'Tuba deo 30g (víčko)', N'1290', N'', N'2019-0027', N'20190027', N'6.3', N'8127.00', N'', '20190731';
  SELECT * FROM MaterialBatch mb WHERE mb.MaterialId = 104 AND mb.Id IN (SELECT Id FROM MaterialBatch)
  
  EXEC sp_importBatch 2, N'Tuba deo 30g (tělo + víčko)', N'1290', N'', N'2019-0027', N'20190027', N'6.3', N'8127.00', N'', '20190731';

  select  PARSE(N'0.474' AS DECIMAL(19,4))


  ROLLBACK;

  
*/

