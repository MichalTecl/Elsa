CREATE PROCEDURE sp_rollbackFinData (@projectId INT, @year INT, @month INT)
AS
BEGIN
BEGIN TRANSACTION;

BEGIN TRY

-- 1. determine the target collections
DECLARE @collectionIds TABLE (Id INT);

INSERT INTO @collectionIds
SELECT Id
FROM InvoiceFormCollection 
WHERE [Year] = @year
AND [Month] = @month
AND ProjectID = @projectId
AND NOT EXISTS(SELECT TOP 1 1 
					FROM InvoiceFormCollection 
				WHERE ProjectId = @projectId
					AND (([Year] * 12) + [Month]) > ((@year * 12) + @month));
IF ((SELECT COUNT(*) FROM @collectionIds) < 1)
BEGIN
   THROW 51000, 'Kolekce neexistuje, nebo neni posledni', 1;
END
	
-- 2. calc counter deltas
DECLARE @counterCorrections TABLE (counterId INT, delta INT);


DECLARE @releaseFormsDefaultCounterId INT = (SELECT TOP 1 Id FROM SystemCounter WHERE ProjectId = @projectId AND Name = 'ReleaseForms_Default');

INSERT INTO @counterCorrections
SELECT COALESCE(f.CounterId, ft.SystemCounterId, @releaseFormsDefaultCounterId), COUNT(f.Id)         
  FROM InvoiceForm f
  JOIN InvoiceFormType ft ON (f.FormTypeId = ft.Id)
 WHERE f.InvoiceFormCollectionId IN (SELECT Id FROM @collectionIds) 
 GROUP BY COALESCE(f.CounterId, ft.SystemCounterId, @releaseFormsDefaultCounterId);


-- 3. Collect elements
DECLARE @invoiceFormIds TABLE (Id INT);
INSERT INTO @invoiceFormIds
SELECT f.Id
  FROM InvoiceForm f
 WHERE f.InvoiceFormCollectionId IN (SELECT c.Id FROM @collectionIds c);

DECLARE @formItemIds TABLE (Id INT);
INSERT INTO @formItemIds
SELECT i.Id
  FROM InvoiceFormItem i
 WHERE i.InvoiceFormId IN (SELECT f.Id FROM @invoiceFormIds f);

-- 4. Delete bridges
PRINT 'Deleting StockEventInvoiceFormItem';
DELETE FROM StockEventInvoiceFormItem WHERE InvoiceFormItemId IN(SELECT x.Id FROM @formItemIds x);

PRINT 'Deleting OrderItemInvoiceFormItem';
DELETE FROM OrderItemInvoiceFormItem WHERE InvoiceFormItemId IN(SELECT x.Id FROM @formItemIds x);

PRINT 'Deleting MaterialBatchCompositionFormItem';
DELETE FROM MaterialBatchCompositionFormItem WHERE InvoiceFormItemId IN(SELECT x.Id FROM @formItemIds x);

PRINT 'Deleting InvoiceFormItemMaterialBatch';
DELETE FROM InvoiceFormItemMaterialBatch WHERE InvoiceFormItemId IN(SELECT x.Id FROM @formItemIds x);

-- 5. Delete items
PRINT 'Deleting InvoiceFormItem';
DELETE FROM InvoiceFormItem WHERE Id IN (SELECT x.Id FROM @formItemIds x);

-- 6. Delete log
PRINT 'Deleting InvoiceFormGenerationLog';
DELETE FROM InvoiceFormGenerationLog WHERE InvoiceFormCollectionId IN (SELECT x.Id FROM @collectionIds x);

-- 7. Delete forms
PRINT 'Deleting InvoiceForm';
DELETE FROM InvoiceForm WHERE Id IN (SELECT x.Id FROM @invoiceFormIds x);

-- 8. Delete collections
PRINT 'Deleting InvoiceFormCollection';
DELETE FROM InvoiceFormCollection WHERE Id IN (SELECT x.Id FROM @collectionIds x);


-- 9. Delete closure
PRINT 'Deleting FinDataGenerationClosure';
delete from FinDataGenerationClosure
  where ProjectId = @projectId
    and [Year] = @year
	and [Month] = @month;

-- 10. rollback counters

WHILE EXISTS(SELECT TOP 1 1 FROM @counterCorrections)
BEGIN
  DECLARE @counterId INT = NULL;
  DECLARE @delta INT = 0;

  SELECT TOP 1 @counterId = counterId, @delta = delta
    FROM @counterCorrections;
  
  DELETE FROM @counterCorrections WHERE counterId = @counterId;

  IF (@counterId IS NULL)
  BEGIN
	THROW 51000, 'Invalid SystemCounter reference', 1;
  END

  DECLARE @kec NVARCHAR(1000) = (SELECT TOP 1 'Updating counter "' + Name + '" from current ' + LTRIM(STR(CounterValue)) + ' to ' + LTRIM(STR(CounterValue - @delta))   FROM SystemCounter WHERE ID = @counterId);

  PRINT @kec;

  UPDATE SystemCounter 
     SET CounterValue = CounterValue - @delta
	WHERE Id = @counterId;	
END

PRINT 'Commiting...';
	COMMIT;
	PRINT '****DONE****';

END TRY
BEGIN CATCH
    PRINT 'CATCH';
	ROLLBACK;
	THROW;
END CATCH
END