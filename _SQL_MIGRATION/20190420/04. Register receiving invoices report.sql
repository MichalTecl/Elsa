DECLARE @projectId INT = 1;

INSERT INTO InvoiceFormReportType
(Name, ViewControlUrl, ViewOrder, ProjectId, DataSourceUrl, GenerateCommandUrl)
SELECT N'Soupisky příjemek', '/UI/Controls/Reporting/Invoicing/InvoiceFormReports/ReceivingInvoicesReport.html', 1,  @ProjectId, '/InvoiceForms/GetReceivingInvoicesCollection', '/InvoiceForms/GenerateReceivingInvoicesCollection'
WHERE NOT EXISTS(SELECT TOP 1 1 FROM InvoiceFormReportType WHERE ProjectId = @ProjectId AND Name = N'Soupisky příjemek');

