using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Warehouse;

namespace Elsa.Apps.Inventory.Model
{
    public class ProducedBatchModel
    {
        public int BatchId { get; set; }

        public string MaterialName { get; set; }

        public BatchStatus Status { get; set; }

        public long PagingDt { get; set; }

        public string DisplayDt { get; set; }

        public string BatchNr { get; set; }

        public string Author { get; set; }

        public string Amount { get; set; }
    }
}
