UPDATE MaterialInventory
   SET ReceivingInvoiceFormGeneratorName = 'PURCHASED'
 WHERE ReceivingInvoiceFormGeneratorName is null 
   AND Name = N'Suroviny';

UPDATE MaterialInventory
   SET ReceivingInvoiceFormGeneratorName = 'MIXTURES'
 WHERE ReceivingInvoiceFormGeneratorName is null 
   AND Name = N'Hmoty';

UPDATE MaterialInventory
   SET ReceivingInvoiceFormGeneratorName = 'PRODUCTS'
 WHERE ReceivingInvoiceFormGeneratorName is null 
   AND Name = N'Výrobky';