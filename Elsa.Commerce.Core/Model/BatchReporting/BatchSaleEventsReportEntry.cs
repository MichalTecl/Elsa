using System.Collections.Generic;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchSaleEventsReportEntry : BatchReportEntryBase
    {
        public BatchSaleEventsReportEntry(BatchKey batchKey) : base(batchKey)
        {
        }

        public List<SaleEventAllocationModel> SaleEvents { get; } = new List<SaleEventAllocationModel>();
    }
}
