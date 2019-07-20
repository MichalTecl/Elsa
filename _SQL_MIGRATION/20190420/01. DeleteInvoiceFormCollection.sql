IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'DeleteInvoiceFormCollection')
	DROP PROCEDURE DeleteInvoiceFormCollection;
GO 

CREATE PROCEDURE DeleteInvoiceFormCollection(@collectionId INT)
AS
BEGIN
	DECLARE @formItems TABLE (id INT);

	INSERT INTO @formItems
	    SELECT itm.Id
	      FROM InvoiceFormCollection coll
    INNER JOIN InvoiceForm           frm  ON (coll.Id = frm.InvoiceFormCollectionId) 
	INNER JOIN InvoiceFormItem       itm  ON (itm.InvoiceFormId = frm.Id)
	     WHERE coll.Id = @collectionId;

    DELETE FROM InvoiceFormGenerationLog WHERE InvoiceFormCollectionId = @collectionId;

	DELETE FROM MaterialBatchCompositionFormItem WHERE InvoiceFormItemId IN (SELECT Id FROM @formItems);
	DELETE FROM BatchStepBatchInvoiceItem        WHERE InvoiceFormItemId IN (SELECT Id FROM @formItems);
	DELETE FROM OrderItemInvoiceFormItem         WHERE InvoiceFormItemId IN (SELECT Id FROM @formItems);
	DELETE FROM InvoiceFormItemMaterialBatch     WHERE InvoiceFormItemId IN (SELECT Id FROM @formItems);
	DELETE FROM StockEventInvoiceFormItem        WHERE InvoiceFormItemId IN (SELECT Id FROM @formItems);
	
	DELETE FROM InvoiceFormItem WHERE Id IN (SELECT Id FROM @formItems);
	DELETE FROM InvoiceForm     WHERE InvoiceFormCollectionId = @collectionId;
	DELETE FROM InvoiceFormCollection WHERE Id = @collectionId;
END