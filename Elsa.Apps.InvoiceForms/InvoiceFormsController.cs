using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Elsa.Apps.InvoiceForms.Facade;
using Elsa.Apps.InvoiceForms.Model;
using Elsa.Apps.InvoiceForms.UiForms;
using Elsa.Apps.Reporting;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Noml.Forms;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;

using Robowire.RoboApi;
using Robowire.RobOrm.Core;
using SelectPdf;

namespace Elsa.Apps.InvoiceForms
{
    [Controller("invoiceForms")]
    public class InvoiceFormsController : ElsaControllerBase
    {
        private readonly ILog m_log;
        private readonly InvoiceFormsQueryingFacade m_facade;
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;
        private readonly IInvoiceFormsGenerationRunner m_generationRunner;
        private readonly ISession m_session;
        private readonly IInvoiceFormRendererFactory m_formRendererFactory;
        private readonly IDatabase m_database;

        public InvoiceFormsController(IWebSession webSession,
            ILog log,
            InvoiceFormsQueryingFacade facade,
            IInvoiceFormsRepository invoiceFormsRepository,
            IInvoiceFormsGenerationRunner generationRunner, 
            IInvoiceFormRendererFactory formRendererFactory, IDatabase database)
            : base(webSession, log)
        {
            m_log = log;
            m_facade = facade;
            m_invoiceFormsRepository = invoiceFormsRepository;
            m_generationRunner = generationRunner;
            m_formRendererFactory = formRendererFactory;
            m_database = database;
            m_session = webSession;
        }

        public InvoiceFormsCollection<ReceivingInvoiceFormModel> GetReceivingInvoicesCollection(int month, int year)
        {
            return GetFormsCollection(month, year, "ReceivingInvoice", item =>
            {
                var model = new ReceivingInvoiceFormModel
                {
                    InvoiceVarSymbol = item.InvoiceVarSymbol ?? item.InvoiceNumber,
                    Supplier = item.Supplier?.Name
                };

                return model;
            });
        }

        public InvoiceFormsCollection<ReleaseFormModel> GetReleaseFormsCollection(int month, int year)
        {
            return GetFormsCollection(month, year, "ReleasingForm", item =>
            {
                var model = new ReleaseFormModel();
                model.Text = item.Text;
                model.InvoiceVarSymbol = item.InvoiceVarSymbol;
                return model;
            });
        }

        public IEnumerable<IInvoiceFormReportType> GetInvoicingReportTypes()
        {
            return m_invoiceFormsRepository.GetInvoiceFormReportTypes();
        }

        public IEnumerable<ReportMonthModel> GetReportMonths()
        {
            var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var lastDate = new DateTime(DateTime.Now.Year - 2, 1, 1);

            for (; startDate >= lastDate; startDate = startDate.AddMonths(-1))
            {
                yield return new ReportMonthModel(startDate.Year, startDate.Month);
            }
        }

        public HtmlResult GetFormHtml(int id)
        {
            var inv = m_invoiceFormsRepository.GetInvoiceFormById(id);

            var renderer = m_formRendererFactory.GetRenderer(inv);

            return new HtmlResult(new Paper(inv.InvoiceFormNumber, renderer));
        }

        public FileResult LoadFormPdf(int id)
        {
            var invoiceForm = m_invoiceFormsRepository.GetInvoiceFormById(id);
            if (invoiceForm == null)
            {
                throw new InvalidOperationException("no access");
            }

            //TODO distinguish generator
            var renderer = m_formRendererFactory.GetRenderer(invoiceForm);
            
            return new FileResult($"{invoiceForm.InvoiceFormNumber}.pdf", renderer.GetPdf(), "application/pdf", "inline");
        }

        public FileResult GetAllForms(int year, int month)
        {
            var formTypes = m_invoiceFormsRepository.GetInvoiceFormTypes().ToList();

            var tempDir = Path.Combine($"C:\\Elsa\\Temp\\{Guid.NewGuid()}\\{month.ToString().PadLeft(2, '0')}-{year}");
            Directory.CreateDirectory(tempDir);

            foreach (var formType in formTypes)
            {
                var ftDir = Path.Combine(tempDir, formType.Name);
                Directory.CreateDirectory(ftDir);

                var collection = m_invoiceFormsRepository.FindCollection(formType.Id, year, month);
                if (collection == null)
                {
                    continue;
                }

                foreach (var form in collection.Forms)
                {
                    var renderer = m_formRendererFactory.GetRenderer(form);
                    var path = Path.Combine(ftDir, $"{form.InvoiceFormNumber}.pdf");
                    File.WriteAllBytes(path, renderer.GetPdf());
                }
            }

            return null;
        }

        public InvoiceFormsCollection<ReceivingInvoiceFormModel> GenerateReceivingInvoicesCollection(int type, int year, int month)
        {
            var x = m_generationRunner.RunReceivingInvoicesGeneration(type, year, month);

            return GetReceivingInvoicesCollection(month, year);
        }

        public InvoiceFormsCollection<ReleaseFormModel> GenerateReleaseFormsCollection(int type, int year, int month)
        {
            // type has probably no sense here, but the method signature needs to be same as GenerateReceivingInvoicesCollection

            var x = m_generationRunner.RunTasks(year, month);

            return GetReleaseFormsCollection(month, year);
        }

        public void ApproveLogWarnings(List<int> ids)
        {
            m_invoiceFormsRepository.ApproveLogWarnings(ids);
        }

        public void DeleteCollection(int id)
        {
            m_invoiceFormsRepository.DeleteCollection(id);
        }

        public void ApproveCollection(int id)
        {
            m_invoiceFormsRepository.ApproveCollection(id);
        }

        public FileResult GetPackage(string cid)
        {
            EnsureUserRight(ReportingUserRights.DownloadInvoicingFormPackages);

            var file = m_database.Sql()
                .ExecuteWithParams(
                    "SELECT TOP 1 PackagePath FROM FinDataGenerationClosure WHERE ProjectId = {0} AND PublicUid = {1}", m_session.Project.Id, cid)
                .Scalar<string>();

            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                throw new InvalidOperationException("Neplatny pozadavek");
            }

            return new FileResult(Path.GetFileName(file), File.ReadAllBytes(file));
        }

        private InvoiceFormsCollection<T> GetFormsCollection<T>(int month, int year, string generatorName,
            Func<IInvoiceForm, T> itemMapper) where T : InvoiceFormModelBase
        {
            var type = m_invoiceFormsRepository.GetInvoiceFormTypes()
                .FirstOrDefault(t => t.GeneratorName == generatorName);
            if (type == null)
            {
                throw new InvalidOperationException($"Nenalezen InvoiceFormType.GeneratroName=ReceivingInvoice");
            }

            return m_facade.Load(type.Id, year, month, itemMapper);
        }
    }
}
