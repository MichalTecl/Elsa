using Elsa.Apps.Inventory.XlsBulkStockEvents;
using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.Inventory
{
    public class InventoryAppRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<BulkStockEventXlsModule>().Use<BulkStockEventXlsModule>();
        }
    }
}
