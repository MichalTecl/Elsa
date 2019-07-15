DECLARE @projectId INT = 1;

IF NOT EXISTS(SELECT TOP 1 1 FROM InvoiceFormType WHERE GeneratorName = N'ReleasingForm')
BEGIN
	INSERT INTO InvoiceFormType (Name, GeneratorName, ProjectId) VALUES (N'Výdejky', N'ReleasingForm', @projectId);
END