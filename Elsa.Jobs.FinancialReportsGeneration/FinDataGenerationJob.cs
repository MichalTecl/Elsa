using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Elsa.App.CommonReports;
using Elsa.Apps.InvoiceForms;
using Elsa.Apps.InvoiceForms.Model;
using Elsa.Apps.InvoiceForms.UiForms;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;
using Elsa.Jobs.Common;
using Elsa.Jobs.FinancialReportsGeneration.Entities;
using Elsa.Smtp.Core;
using Robowire.RobOrm.Core;
using XlsSerializer.Core;

namespace Elsa.Jobs.FinancialReportsGeneration
{
    public class FinDataGenerationJob : IExecutableJob
    {
        private readonly ILog _log;
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly IInvoiceFormsGenerationRunner _formsGenerationRunner;
        private readonly IInvoiceFormsRepository _formsRepository;
        private readonly IInvoiceFormRendererFactory _formRendererFactory;
        private readonly IStockReportLoader _stockReportLoader;
        private readonly InvoiceFormsController _controller;
        private readonly IMailSender _mailSender;

        public FinDataGenerationJob(ILog log, IDatabase database, ISession session,
            IInvoiceFormsGenerationRunner formsGenerationRunner, IInvoiceFormsRepository formsRepository,
            IInvoiceFormRendererFactory formRendererFactory, IStockReportLoader stockReportLoader, InvoiceFormsController controller, IMailSender mailSender)
        {
            _log = log;
            _database = database;
            _session = session;
            _formsGenerationRunner = formsGenerationRunner;
            _formsRepository = formsRepository;
            _formRendererFactory = formRendererFactory;
            _stockReportLoader = stockReportLoader;
            _controller = controller;
            _mailSender = mailSender;
        }

        public void Run(string customDataJson)
        {
            try
            {
                DateTime lastExisting = DateTime.Now.AddMonths(-2);

                _log.Info("Checking for last existing findata");

                _database.Sql()
                    .ExecuteWithParams("SELECT TOP 1 [Year], [Month] FROM FinDataGenerationClosure WHERE ProjectId={0} ORDER BY closeDt DESC", _session.Project.Id)
                    .ReadRows<int, int>((lyear, lmonth) =>
                    {
                        lastExisting = new DateTime(lyear, lmonth, 1);
                        _log.Info($"In DB last existing findata generation = {lastExisting}");
                    });

                _log.Info($"Last existing findat considered to be from {lastExisting}");

                var now = DateTime.Now;
                var generateFor = lastExisting.AddMonths(1);

                while (!((generateFor.Year == now.Year) && (generateFor.Month == now.Month)))
                {
                    _log.Info($"Starting generation of findata for {generateFor.Year}/{generateFor.Month:00}");

                    Generate(generateFor);

                    generateFor = generateFor.AddMonths(1);
                }

                _log.Info("Last generated findata are for prev month - done");
            }
            catch (Exception ex)
            {
                _log.Error("Run failed", ex);
                throw;
            }
        }

        private void Generate(DateTime now)
        {
            var year = now.Year;
            var month = now.Month;
            _log.Info($"Starting FinDataGeneration job for Year={year} Month={month}");

            var messages = new List<string>();

            StartGeneration(year, month, m =>
            {
                if (!messages.Contains(m))
                {
                    messages.Add(m);
                }
            }, () => SendNotification(messages, month, year));

            SendNotification(messages, month, year);
        }

        private void SendNotification(List<string> messages, int month, int year)
        {
            if (!messages.Any())
            {
                _log.Info("No messages collected - email notification skipped");
                return;
            }

            var sb = new StringBuilder();
            while (messages.Any())
            {
                sb.AppendLine(messages[0]);
                messages.RemoveAt(0);
            }

            _log.Info($"mail message created: {sb}");

            _mailSender.SendToGroup(SenderMailboxType.SystemRobot, "Ucetni Vystupy", $"Účetní výstupy {month:00}/{year}", sb.ToString());

        }

        private void StartGeneration(int year, int month, Action<string> mailReport, Action flushReport)
        {
            try
            {
            
                _log.Info("Checking closure");

                var closure = GetOrUpdateClosure(year, month);

                if (closure == null)
                {
                    _log.Info("Closure for {month}/{year} not found. Starting forms generation");

                    if (!ProcessCollections(year, month, mailReport))
                    {
                        return;
                    }

                    closure = GetOrUpdateClosure(year, month, cl => { cl.CloseDt = DateTime.Now; });
                }

                if (string.IsNullOrWhiteSpace(closure.PackagePath) || !File.Exists(closure.PackagePath))
                {
                    var path = GeneratePackage(year, month, mailReport);
                    closure = GetOrUpdateClosure(year, month, cl => { cl.PackagePath = path; });
                }

                if (closure.NotificationDt == null)
                {
                    mailReport(
                        $"Balíček výstupních souborů byl úspěšně vygenerován a je připraven ke stažení: {_session.Project.HomeUrl}/invoiceForms/getpackage?cid={closure.PublicUid}");
                    flushReport();

                    GetOrUpdateClosure(year, month, u => u.NotificationDt = DateTime.Now);
                }
                
                _log.Info($"Closure complete for {month}/{year}");
            }
            catch (Exception ex)
            {
                _log.Error("FinDataGenerationJob failed", ex);
                mailReport($"Při pokusu o generování účetních dat došlo k chybě, která neumožňuje pokračovat: {ex.Message}");
            }
        }

        private string GeneratePackage(int year, int month, Action<string> mailReport)
        {
            var tempDir = Path.Combine($"C:\\Elsa\\Temp\\FinReportPackages\\{_session.Project.Name}\\{month.ToString().PadLeft(2, '0')}-{year}");

            _log.Info($"Starting package generation. Target = {tempDir}");

            if (Directory.Exists(tempDir))
            {
                _log.Info($"{tempDir} already exists, deleting");
                Directory.Delete(tempDir, true);
            }

            Directory.CreateDirectory(tempDir);
            _log.Info($"{tempDir} created");

            SaveCollections(year, month, tempDir);

            _log.Info("Loading InvoiceFormTypes");
            var formTypes = _formsRepository.GetInvoiceFormTypes().ToList();
            _log.Info($"Loaded {formTypes.Count} InvoiceFormTypes: [{string.Join(", ", formTypes.Select(ft => ft.Name))}]");

            foreach (var formType in formTypes)
            {
                _log.Info($"Loading collecton for formType.Name = {formType.Name}, formType.Id={formType.Id}, year={year}, month={month}");

                var collection = _formsRepository.FindCollection(formType.Id, year, month);
                if (collection == null)
                {
                    _log.Error($"Collection not found!");
                    continue;
                }

                _log.Info($"Collection loaded: {collection.Forms.Count()} forms");

                var ftDir = Path.Combine(tempDir, StringUtil.ReplaceNationalChars(formType.Name));
                Directory.CreateDirectory(ftDir);
                _log.Info($"Created directory {ftDir}");

                var i = 0;
                foreach (var form in collection.Forms)
                {
                    i++;
                    var renderer = _formRendererFactory.GetRenderer(form);
                    var path = Path.Combine(ftDir, $"{form.InvoiceFormNumber}.pdf");
                    File.WriteAllBytes(path, renderer.GetPdf());
                }
                _log.Info($"{i} files generated");

                var zipTarget = $"{ftDir}.zip";

                if (File.Exists(zipTarget))
                {
                    _log.Info($"Zip archive {zipTarget} already exists - deleting");
                    File.Delete(zipTarget);
                }

                _log.Info($"Creating archive {zipTarget}");
                ZipFile.CreateFromDirectory(ftDir, zipTarget);
                _log.Info("Archive created");
                Directory.Delete(ftDir, true);
            }

            _log.Info("All form types processed");

            SaveStockReport(year, month, tempDir);
            
            var packagePath = $"{tempDir}.zip";
            _log.Info($"Compressing {tempDir} to {packagePath}");
            if (File.Exists(packagePath))
            {
                _log.Info($"{packagePath} already exists - deleting");
                File.Delete(packagePath);
            }

            ZipFile.CreateFromDirectory(tempDir, packagePath);
            _log.Info($"Package created at {packagePath}");

            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                _log.Error($"Cannot delete {tempDir}", ex);
                throw;
            }

            return packagePath;
        }

        private void SaveCollections(int year, int month, string tempDir)
        {
            SaveCollection(_controller.GetReceivingInvoicesCollection(month, year).GetExcelModel<ReceivingFormXlsModel>().ToList(), tempDir, "SoupiskaPrijemek.xlsx");
            SaveCollection(_controller.GetReleaseFormsCollection(month, year).GetExcelModel<ReleaseFormXlsModel>().ToList(), tempDir, "SoupiskaVydejek.xlsx");
        }

        private void SaveCollection<T>(IList<T> items, string path, string fileName)
        {
            var targetPath = Path.Combine(path, fileName);

            _log.Info($"Serializing to {targetPath}");
            XlsxSerializer.Instance.Serialize(items, targetPath);
            _log.Info($"Saved {targetPath}");
        }

        private void SaveStockReport(int year, int month, string root)
        {
            var rd = new DateTime(year, month,1).AddMonths(1).AddSeconds(-1);
            _log.Info("Starting generating stockReport for {rd}");

            var stockReport = _stockReportLoader.LoadStockReport(rd);
            var bytes = XlsxSerializer.Instance.Serialize(stockReport);

            var tarPath = Path.Combine(root, $"Stav skladu {StringUtil.FormatDate(rd)}.xlsx");
            _log.Info($"Saving stockReport to {tarPath}");
            File.WriteAllBytes(tarPath, bytes);
        }

        private bool ProcessCollection(int year, int month, OverviewRow row, Action<string> mailReport)
        {
            while (true)
            {
                _log.Info($"Processing {row}");

                if (row.IsGenerated)
                {
                    if (row.IsApproved)
                    {
                        _log.Info($"{row.GeneratorName} is generated and approved OK");
                        return true;
                    }
                    else 
                    {
                        if (!row.CanApprove)
                        {
                            mailReport(
                                $"Účetní data není možné vygenerovat, protože log generátoru reportu {row.FormTypeName} {month:00}/{year} obsahuje varování, která musí být vyřešena ručně.");
                            return false;
                        }

                        _log.Info("Starting collection approval");
                        _formsRepository.ApproveCollection(row.CollectionId.Ensure("Unexpected CollectionId=null"));
                    }
                }
                else
                {
                    _log.Info("Starting form generation");

                    if (row.GeneratorName.Equals("ReceivingInvoice"))
                    {
                        _formsGenerationRunner.RunReceivingInvoicesGeneration(row.FormTypeId, year, month);
                    }
                    else
                    {
                        _formsGenerationRunner.RunTasks(year, month);
                    }
                }

                row = LoadOverview(year, month).Single(ovw => ovw.FormTypeId == row.FormTypeId);
            }
        }

        private bool ProcessCollections(int year, int month, Action<string> mailReport)
        {
            var result = true;

            foreach (var overwiewRow in LoadOverview(year, month))
            {
                if (!ProcessCollection(year, month, overwiewRow, mailReport))
                {
                    result = false;
                }
            }

            return result;
        }

        private IFinDataGenerationClosure GetOrUpdateClosure(int year, int month, Action<IFinDataGenerationClosure> updater = null)
        {
            var closure = _database.SelectFrom<IFinDataGenerationClosure>()
                .Where(c => c.ProjectId == _session.Project.Id && c.Year == year && c.Month == month).Take(1)
                .Execute()
                .FirstOrDefault();

            if (updater != null)
            {
                if (closure == null)
                {
                    closure = _database.New<IFinDataGenerationClosure>();
                    closure.ProjectId = _session.Project.Id;
                    closure.Year = year;
                    closure.Month = month;
                    closure.PublicUid = Guid.NewGuid().ToString("N");
                }

                updater(closure);

                _database.Save(closure);
            }

            return closure;
        }

        private List<OverviewRow> LoadOverview(int year, int month)
        {
            var report = new List<OverviewRow>(10);

            _database.Sql().Call("GetFinFormsGenerationOverview").WithParam("@projectId", _session.Project.Id)
                .WithParam("@year", year).WithParam("@month", month).ReadRows<int, string, string, bool, bool, bool, int?>(
                    (typeId, formName, generatorName, isGenerated, isApproved, canApprove, collectionId) =>
                    {
                        report.Add(new OverviewRow
                        {
                            FormTypeId = typeId,
                            FormTypeName = formName,
                            GeneratorName = generatorName,
                            IsGenerated = isGenerated,
                            IsApproved = isApproved,
                            CanApprove = canApprove,
                            CollectionId = collectionId
                        });
                    });

            return report;
        }

        private class OverviewRow
        {
            public int FormTypeId { get; set; }

            public string FormTypeName { get; set; }

            public string GeneratorName { get; set; }

            public bool IsGenerated { get; set; }

            public bool IsApproved { get; set; }

            public bool CanApprove { get; set; }

            public int? CollectionId { get; set; }

            public override string ToString()
            {
                return
                    $"{FormTypeName} Generated={IsGenerated} Approved={IsApproved} CanApprove={CanApprove} CollectionId={CollectionId}";
            }
        }
    }
}
