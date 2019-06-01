using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Elsa.Apps.InvoiceForms.Facade;
using Elsa.Apps.InvoiceForms.Model;
using Elsa.Apps.InvoiceForms.UiForms;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Noml.Forms;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Invoicing.Core.Data;

using Robowire.RoboApi;

namespace Elsa.Apps.InvoiceForms
{
    [Controller("invoiceForms")]
    public class InvoiceFormsController : ElsaControllerBase
    {
        private readonly ILog m_log;
        private readonly InvoiceFormsQueryingFacade m_facade;
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;

        public InvoiceFormsController(IWebSession webSession,
            ILog log,
            InvoiceFormsQueryingFacade facade,
            IInvoiceFormsRepository invoiceFormsRepository)
            : base(webSession, log)
        {
            m_log = log;
            m_facade = facade;
            m_invoiceFormsRepository = invoiceFormsRepository;
        }

        public InvoiceFormsCollection<ReceivingInvoiceFormModel> GetReceivingInvoicesForm(int? month, int? year)
        {
            var type = m_invoiceFormsRepository.GetInvoiceFormTypes()
                .FirstOrDefault(t => t.GeneratorName == "ReceivingInvoice");
            if (type == null)
            {
                throw new InvalidOperationException($"Nenalezen InvoiceFormType.GeneratroName=ReceivingInvoice");
            }

            return m_facade.Load(type.Id, year, month,
                item =>
                {
                    var model = new ReceivingInvoiceFormModel();
                    model.InvoiceVarSymbol = item.InvoiceVarSymbol ?? item.InvoiceNumber;
                    model.Supplier = item.Supplier?.Name; 
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

        public HtmlResult DownloadReceivingInvoice(int id)
        {
            var inv = m_invoiceFormsRepository.GetInvoiceFormById(id);

            var renderer = new ReceivingInvoiceFormRenderer(inv);

            return new HtmlResult(new Paper(inv.InvoiceFormNumber, renderer));
        }

        public FileResult DownloadInvoiceForm(int id)
        {
            var invoiceForm = m_invoiceFormsRepository.GetInvoiceFormById(id);
            if (invoiceForm == null)
            {
                throw new InvalidOperationException("no access");
            }

            //TODO distinguish generator
            var renderer = new ReceivingInvoiceFormRenderer(invoiceForm);
            
            var fileName = $"{invoiceForm.InvoiceFormNumber}.pdf";

            return new FileResult(fileName, renderer.GetPdf());
        }
    }
}
