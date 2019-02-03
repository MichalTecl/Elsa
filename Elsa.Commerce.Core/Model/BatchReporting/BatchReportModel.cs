using System.Collections.Generic;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchReportModel
    {
        public BatchReportQuery Query { get; set; }

        public bool IsUpdate { get; set; }

        public List<BatchReportEntryBase> Report { get; set; } = new List<BatchReportEntryBase>();
        public bool CanLoadMore { get; set; }
        public string CustomField1Name{ get; set; }

        public string CustomField2Name { get; set; }

        public string CustomField3Name { get; set; }
    }
}
