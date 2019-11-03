using System.Collections.Generic;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchOrdersReportEntry : BatchReportEntryBase
    {
        public BatchOrdersReportEntry(BatchKey batchKey)
            : base(batchKey)
        {
        }

        public List<BatchOrderModel> Orders { get; } = new List<BatchOrderModel>();

        public bool CanLoadMoreOrders { get; set; }

        public int NextOrdersPage { get; set; }

        public bool BatchesExpanded { get; set; } = false;
    }
}
