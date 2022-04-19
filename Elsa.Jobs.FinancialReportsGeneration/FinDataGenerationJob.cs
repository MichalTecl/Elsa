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
        private readonly ILog m_log;
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly IInvoiceFormsGenerationRunner m_formsGenerationRunner;
        private readonly IInvoiceFormsRepository m_formsRepository;
        private readonly IInvoiceFormRendererFactory m_formRendererFactory;
        private readonly IStockReportLoader m_stockReportLoader;
        private readonly InvoiceFormsController m_controller;
        private readonly IMailSender m_mailSender;

        public FinDataGenerationJob(ILog log, IDatabase database, ISession session,
            IInvoiceFormsGenerationRunner formsGenerationRunner, IInvoiceFormsRepository formsRepository,
            IInvoiceFormRendererFactory formRendererFactory, IStockReportLoader stockReportLoader, InvoiceFormsController controller, IMailSender mailSender)
        {
            m_log = log;
            m_database = database;
            m_session = session;
            m_formsGenerationRunner = formsGenerationRunner;
            m_formsRepository = formsRepository;
            m_formRendererFactory = formRendererFactory;
            m_stockReportLoader = stockReportLoader;
            m_controller = controller;
            m_mailSender = mailSender;
        }

        public void Run(string customDataJson)
        {
            try
            {
                DateTime lastExisting = DateTime.Now.AddMonths(-2);

                m_log.Info("Checking for last existing findata");

                m_database.Sql()
                    .ExecuteWithParams("SELECT TOP 1 [Year], [Month] FROM FinDataGenerationClosure WHERE ProjectId={0} ORDER BY closeDt DESC", m_session.Project.Id)
                    .ReadRows<int, int>((lyear, lmonth) =>
                    {
                        lastExisting = new DateTime(lyear, lmonth, 1);
                        m_log.Info($"In DB last existing findata generation = {lastExisting}");
                    });

                m_log.Info($"Last existing findat considered to be from {lastExisting}");

                var now = DateTime.Now;
                var generateFor = lastExisting.AddMonths(1);

                while (!((generateFor.Year == now.Year) && (generateFor.Month == now.Month)))
                {
                    m_log.Info($"Starting generation of findata for {generateFor.Year}/{generateFor.Month:00}");

                    Generate(generateFor);

                    generateFor = generateFor.AddMonths(1);
                }

                m_log.Info("Last generated findata are for prev month - done");
            }
            catch (Exception ex)
            {
                m_log.Error("Run failed", ex);
                throw;
            }
        }

        private void Generate(DateTime now)
        {
            var year = now.Year;
            var month = now.Month;
            m_log.Info($"Starting FinDataGeneration job for Year={year} Month={month}");

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
                m_log.Info("No messages collected - email notification skipped");
                return;
            }

            var sb = new StringBuilder();
            while (messages.Any())
            {
                sb.AppendLine(messages[0]);
                messages.RemoveAt(0);
            }

            m_log.Info($"mail message created: {sb}");

            m_mailSender.SendToGroup("Ucetni Vystupy", $"Účetní výstupy {month:00}/{year}", sb.ToString());

        }

        private void StartGeneration(int year, int month, Action<string> mailReport, Action flushReport)
        {
            try
            {
            
                m_log.Info("Checking closure");

                var closure = GetOrUpdateClosure(year, month);

                if (closure == null)
                {
                    m_log.Info("Closure for {month}/{year} not found. Starting forms generation");

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
                        $"Balíček výstupních souborů byl úspěšně vygenerován a je připraven ke stažení: {m_session.Project.HomeUrl}/invoiceForms/getpackage?cid={closure.PublicUid}");
                    flushReport();

                    GetOrUpdateClosure(year, month, u => u.NotificationDt = DateTime.Now);
                }
                
                m_log.Info($"Closure complete for {month}/{year}");
            }
            catch (Exception ex)
            {
                m_log.Error("FinDataGenerationJob failed", ex);
                mailReport($"Při pokusu o generování účetních dat došlo k chybě, která neumožňuje pokračovat: {ex.Message}");
            }
        }

        private string GeneratePackage(int year, int month, Action<string> mailReport)
        {
            var tempDir = Path.Combine($"C:\\Elsa\\Temp\\FinReportPackages\\{m_session.Project.Name}\\{month.ToString().PadLeft(2, '0')}-{year}");

            m_log.Info($"Starting package generation. Target = {tempDir}");

            if (Directory.Exists(tempDir))
            {
                m_log.Info($"{tempDir} already exists, deleting");
                Directory.Delete(tempDir, true);
            }

            Directory.CreateDirectory(tempDir);
            m_log.Info($"{tempDir} created");

            SaveCollections(year, month, tempDir);

            var formTypes = m_formsRepository.GetInvoiceFormTypes().ToList();

            foreach (var formType in formTypes)
            {
                var collection = m_formsRepository.FindCollection(formType.Id, year, month);
                if (collection == null)
                {
                    m_log.Error($"Collection not found!");
                    continue;
                }

                var ftDir = Path.Combine(tempDir, StringUtil.ReplaceNationalChars(formType.Name));
                Directory.CreateDirectory(ftDir);
                m_log.Info($"Created directory {ftDir}");

                var i = 0;
                foreach (var form in collection.Forms)
                {
                    i++;
                    var renderer = m_formRendererFactory.GetRenderer(form);
                    var path = Path.Combine(ftDir, $"{form.InvoiceFormNumber}.pdf");
                    File.WriteAllBytes(path, renderer.GetPdf());
                }
                m_log.Info($"{i} files generated");

                var zipTarget = $"{ftDir}.zip";

                if (File.Exists(zipTarget))
                {
                    m_log.Info($"Zip archive {zipTarget} already exists - deleting");
                    File.Delete(zipTarget);
                }

                m_log.Info($"Creating archive {zipTarget}");
                ZipFile.CreateFromDirectory(ftDir, zipTarget);
                m_log.Info("Archive created");
                Directory.Delete(ftDir, true);
            }

            SaveStockReport(year, month, tempDir);
            
            var packagePath = $"{tempDir}.zip";
            m_log.Info($"Compressing {tempDir} to {packagePath}");
            if (File.Exists(packagePath))
            {
                m_log.Info($"{packagePath} already exists - deleting");
                File.Delete(packagePath);
            }

            ZipFile.CreateFromDirectory(tempDir, packagePath);
            m_log.Info($"Package created at {packagePath}");

            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                m_log.Error($"Cannot delete {tempDir}", ex);
                throw;
            }

            return packagePath;
        }

        private void SaveCollections(int year, int month, string tempDir)
        {
            SaveCollection(m_controller.GetReceivingInvoicesCollection(month, year).GetExcelModel<ReceivingFormXlsModel>().ToList(), tempDir, "SoupiskaPrijemek.xlsx");
            SaveCollection(m_controller.GetReleaseFormsCollection(month, year).GetExcelModel<ReleaseFormXlsModel>().ToList(), tempDir, "SoupiskaVydejek.xlsx");
        }

        private void SaveCollection<T>(IList<T> items, string path, string fileName)
        {
            var targetPath = Path.Combine(path, fileName);

            m_log.Info($"Serializing to {targetPath}");
            XlsxSerializer.Instance.Serialize(items, targetPath);
            m_log.Info($"Saved {targetPath}");
        }

        private void SaveStockReport(int year, int month, string root)
        {
            var rd = new DateTime(year, month,1).AddMonths(1).AddSeconds(-1);
            m_log.Info("Starting generating stockReport for {rd}");

            var stockReport = m_stockReportLoader.LoadStockReport(rd);
            var bytes = XlsxSerializer.Instance.Serialize(stockReport);

            var tarPath = Path.Combine(root, $"Stav skladu {StringUtil.FormatDate(rd)}.xlsx");
            m_log.Info($"Saving stockReport to {tarPath}");
            File.WriteAllBytes(tarPath, bytes);
        }

        private bool ProcessCollection(int year, int month, OverviewRow row, Action<string> mailReport)
        {
            while (true)
            {
                m_log.Info($"Processing {row}");

                if (row.IsGenerated)
                {
                    if (row.IsApproved)
                    {
                        m_log.Info($"{row.GeneratorName} is generated and approved OK");
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

                        m_log.Info("Starting collection approval");
                        m_formsRepository.ApproveCollection(row.CollectionId.Ensure("Unexpected CollectionId=null"));
                    }
                }
                else
                {
                    m_log.Info("Starting form generation");

                    if (row.GeneratorName.Equals("ReceivingInvoice"))
                    {
                        m_formsGenerationRunner.RunReceivingInvoicesGeneration(row.FormTypeId, year, month);
                    }
                    else
                    {
                        m_formsGenerationRunner.RunTasks(year, month);
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
            var closure = m_database.SelectFrom<IFinDataGenerationClosure>()
                .Where(c => c.ProjectId == m_session.Project.Id && c.Year == year && c.Month == month).Take(1)
                .Execute()
                .FirstOrDefault();

            if (updater != null)
            {
                if (closure == null)
                {
                    closure = m_database.New<IFinDataGenerationClosure>();
                    closure.ProjectId = m_session.Project.Id;
                    closure.Year = year;
                    closure.Month = month;
                    closure.PublicUid = Guid.NewGuid().ToString("N");
                }

                updater(closure);

                m_database.Save(closure);
            }

            return closure;
        }

        private List<OverviewRow> LoadOverview(int year, int month)
        {
            var report = new List<OverviewRow>(10);

            m_database.Sql().Call("GetFinFormsGenerationOverview").WithParam("@projectId", m_session.Project.Id)
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
