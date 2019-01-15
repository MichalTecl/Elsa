using System.Collections.Generic;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchReportModel
    {
        public BatchReportQuery Query { get; set; }

        public List<BatchReportEntry> Report { get; set; } = new List<BatchReportEntry>();
        public bool CanLoadMore { get; set; }
    }
}
