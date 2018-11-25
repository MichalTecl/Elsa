using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.Inventory.Model
{
    public class BatchSetupRequest
    {
        public int? BatchId { get; set; }

        public string MaterialName { get; set; }

        public string BatchNumber { get; set; }

        public decimal Amount { get; set; }

        public string AmountUnitSymbol { get; set; }
    }
}
