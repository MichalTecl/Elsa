using Elsa.Commerce.Core;
using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.ImportOrders
{
    public class ImportOrdersJobRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IAdHocOrdersSyncProvider>().Use<ImportOrdersJob>();
        }
    }
}
