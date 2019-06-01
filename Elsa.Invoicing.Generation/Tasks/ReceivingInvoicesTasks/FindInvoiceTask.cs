using System.Linq;

using Elsa.Common.Logging;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Invoicing.Generation.Tasks.ReceivingInvoicesTasks
{
    public class FindInvoiceTask : IGenerationTask
    {
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;
        private readonly ILog m_log;

        public FindInvoiceTask(IInvoiceFormsRepository invoiceFormsRepository, ILog log)
        {
            m_invoiceFormsRepository = invoiceFormsRepository;
            m_log = log;
        }

        public void Run(GenerationContext context)
        {
            m_log.Info($"Hledam prijemku podle: SupplierId:{context.SourceBatch.SupplierId} && InvoiceNr:{context.SourceBatch.InvoiceNr}'");

            if (string.IsNullOrWhiteSpace(context.SourceBatch.InvoiceNr))
            {
                m_log.Info("Nehleda se existujici prijemka, protoze cislo faktury je prazdne");
                return;
            }

            var invoice = m_invoiceFormsRepository.FindInvoiceForms(context.InvoiceFormTypeId,
                null,
                context.SourceBatch.InvoiceNr,
                context.SourceBatch.SupplierId, null, null).LastOrDefault();

            if (invoice != null)
            {
                context.InvoiceForm = invoice;
                m_log.Info($"Nalezena prijemka {invoice.InvoiceFormNumber}");
            }
            else
            {
                m_log.Info("Prijemka nenalezena");
            }
        }
    }
}
