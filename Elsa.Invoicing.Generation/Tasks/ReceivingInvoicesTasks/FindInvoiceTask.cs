using System.Linq;

using Elsa.Invoicing.Core.Data;

namespace Elsa.Invoicing.Generation.Tasks.ReceivingInvoicesTasks
{
    public class FindInvoiceTask : IGenerationTask
    {
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;

        public FindInvoiceTask(IInvoiceFormsRepository invoiceFormsRepository)
        {
            m_invoiceFormsRepository = invoiceFormsRepository;
        }

        public void Run(GenerationContext context)
        {
            context.InvoiceForm = m_invoiceFormsRepository.FindInvoiceForms(context.InvoiceFormTypeId,
                null,
                context.SourceBatch.InvoiceNr,
                context.SourceBatch.SupplierId).LastOrDefault();
        }
    }
}
