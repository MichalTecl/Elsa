using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.Invoices.Model;

namespace Elsa.Apps.Invoices
{
    public interface IInvoiceFileProcessor
    {
        void ProcessFile(InvoiceModel model);
    }
}
