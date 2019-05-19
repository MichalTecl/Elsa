using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Invoicing.Generation.Tasks.ReceivingInvoicesTasks
{
    public class SaveInvoiceTask : IGenerationTask
    {
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;

        public SaveInvoiceTask(IInvoiceFormsRepository invoiceFormsRepository)
        {
            m_invoiceFormsRepository = invoiceFormsRepository;
        }

        public void Run(GenerationContext context)
        {
            context.SaveInvoice(m_invoiceFormsRepository);
        }
    }
}
