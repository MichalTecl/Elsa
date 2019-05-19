using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;

using Robowire;

namespace Elsa.Invoicing.Core
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IInvoiceFormsRepository>().Use<InvoiceFormRepository>();
        }
    }
}
