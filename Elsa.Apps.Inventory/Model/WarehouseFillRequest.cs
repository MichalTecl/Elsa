using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.Inventory.Model
{
    public class WarehouseFillRequest
    {
        public string MaterialName { get; set; }

        public decimal Amount { get; set; }

        public string UnitName { get; set; }

        public string Note { get; set; }
    }
}
