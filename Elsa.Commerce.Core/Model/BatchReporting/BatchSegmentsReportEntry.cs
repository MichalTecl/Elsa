using System.Collections.Generic;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchSegmentsReportEntry : BatchReportEntryBase
    {
        public BatchSegmentsReportEntry(BatchKey batchKey) : base(batchKey)
        {
        }

        public List<BatchSegmentModel> Segments { get; } = new List<BatchSegmentModel>();
    }
}
