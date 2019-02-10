using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public abstract class BatchReportEntryBase
    {
        protected BatchReportEntryBase(int batchId)
        {
            BatchId = batchId;
        }

        public int BatchId { get; }

        public int? ParentId { get; set; }

        public bool CanDelete { get; set; }

        public string NoDelReason { get; set; }
    }
}
