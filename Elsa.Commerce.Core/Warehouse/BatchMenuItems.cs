using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Model.BatchReporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.Inventory.Model
{
    public class BatchMenuItems
    {
        public string BatchKey { get; set; }
        public List<BatchStockEventSuggestion> EventSuggestions { get; } = new List<BatchStockEventSuggestion>();
        public List<OneClickProductionOption> ProductionSuggestions { get; } = new List<OneClickProductionOption>();
    }
}
