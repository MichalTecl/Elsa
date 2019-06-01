using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Utils;

namespace Elsa.Invoicing.Generation.Tasks.ReceivingInvoicesTasks
{
    public class PopulateInvoiceTask : IGenerationTask
    {
        public void Run(GenerationContext context)
        {
            var batch = context.SourceBatch;
            var invoice = context.InvoiceForm;

            //InvoiceNumber
            if (!string.IsNullOrWhiteSpace(batch.InvoiceNr))
            {
                invoice.SetAndThrowIfReassign(i => i.InvoiceNumber, batch.InvoiceNr);
            }

            //InvoiceVarSymbol
            if (!string.IsNullOrWhiteSpace(batch.InvoiceVarSymbol))
            {
                invoice.SetAndThrowIfReassign(i => i.InvoiceVarSymbol, batch.InvoiceVarSymbol);
            }

            //IssueDate
            invoice.SetAndThrowIfReassign(i => i.IssueDate, batch.Created.Date);

            //SupplierId
            if (batch.SupplierId != null)
            {
                invoice.SetAndThrowIfReassign(i => i.SupplierId, batch.SupplierId);
            }
        }
    }
}
