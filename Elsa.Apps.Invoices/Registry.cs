using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robowire;

namespace Elsa.Apps.Invoices
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<InvoiceModelFactory>().Use<InvoiceModelFactory>();
            setup.For<IInvoiceFileProcessor>().Use<InvoiceModelProcessor>();
        }
    }
}
