DECLARE @projectId INT = 1;

IF NOT EXISTS(SELECT TOP 1 1 FROM InvoiceFormType WHERE Name = N'Příjemky')
BEGIN
	INSERT INTO InvoiceFormType (Name, GeneratorName, ProjectId) VALUES (N'Příjemky', N'ReceivingInvoice', @projectId);
END

DECLARE @counterId INT;

IF NOT EXISTS(SELECT TOP 1 1 FROM SystemCounter WHERE Name = 'ReceivingInvoice')
BEGIN
	INSERT INTO SystemCounter (Name, StaticPrefix, DtFormat, CounterValue, CounterMinValue, CounterPadding, ProjectId)
	VALUES ('ReceivingInvoice', 'P', 'yyyy', 0, 1, 5, @projectId);	
END

UPDATE InvoiceFormType
   SET SystemCounterId = (SELECT TOP 1 Id FROM SystemCounter WHERE Name = 'ReceivingInvoice')
 WHERE ProjectId = @projectId
   AND GeneratorName = 'ReceivingInvoice'
   AND SystemCounterId IS NULL;
 






