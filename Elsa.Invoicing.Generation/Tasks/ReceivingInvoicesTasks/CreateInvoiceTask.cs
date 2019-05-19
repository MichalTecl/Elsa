using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Invoicing.Core.Data;

namespace Elsa.Invoicing.Generation.Tasks.ReceivingInvoicesTasks
{
    public class CreateInvoiceTask : IGenerationTask
    {
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;

        public CreateInvoiceTask(IInvoiceFormsRepository invoiceFormsRepository)
        {
            m_invoiceFormsRepository = invoiceFormsRepository;
        }

        public void Run(GenerationContext context)
        {
            if (context.InvoiceForm != null)
            {
                return;
            }

            context.InvoiceForm = m_invoiceFormsRepository.GetTemplate(context.InvoiceFormTypeId, f =>
                {
                    f.InvoiceNumber = context.SourceBatch.InvoiceNr;
                    f.SupplierId = context.SourceBatch.SupplierId;
                });
        }
    }
}
