DECLARE @projectId INT = 1;

INSERT INTO InvoiceFormReportType
(Name, ViewControlUrl, ViewOrder, ProjectId, DataSourceUrl, GenerateCommandUrl)
SELECT N'Soupisky výdejek', '/UI/Controls/Reporting/Invoicing/InvoiceFormReports/ReleaseFormsReport.html', 2,  @ProjectId, '/InvoiceForms/GetReleaseFormsCollection', '/InvoiceForms/GenerateReleaseFormsCollection'
WHERE NOT EXISTS(SELECT TOP 1 1 FROM InvoiceFormReportType WHERE ProjectId = @ProjectId AND Name = N'Soupisky výdejek');