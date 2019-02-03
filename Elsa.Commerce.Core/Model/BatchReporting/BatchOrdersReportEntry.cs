using System.Collections.Generic;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchOrdersReportEntry : BatchReportEntryBase
    {
        public BatchOrdersReportEntry(int batchId)
            : base(batchId)
        {
        }

        public List<BatchOrderModel> Orders { get; } = new List<BatchOrderModel>();

        public bool CanLoadMoreOrders { get; set; }

        public int NextOrdersPage { get; set; }

        public bool BatchesExpanded { get; set; } = false;
    }
}
