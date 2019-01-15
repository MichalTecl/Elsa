using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchReportEntry
    {
        public int BatchId { get; set; }
        public string InventoryName { get; set; }
        public string BatchNumber { get; set; }
        public string MaterialName { get; set; }
        public int MaterialId { get; set; }
        public string BatchVolume { get; set; }
        public string AvailableAmount { get; set; }
        public string CreateDt { get; set; }
        public bool IsClosed { get; set; }
        public bool IsLocked { get; set; }
        public bool IsAvailable { get; set; }
        public bool AllStepsDone { get; set; }
        public int NumberOfComponents { get; set; }
        public int NumberOfCompositions { get; set; }
        public int NumberOfRequiredSteps { get; set; }
        public int NumberOfOrders { get; set; }
    }
}
