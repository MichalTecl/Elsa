using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public abstract class BatchReportEntryBase
    {
        protected BatchReportEntryBase(BatchKey batchKey)
        {
            //To ensure key was resolved
            batchKey.UnsafeToString();

            BatchKey = batchKey;
        }

        public BatchKey BatchKey { get; }

        public string BatchId => BatchKey.UnsafeToString();

        public int? ParentId { get; set; }

        public bool CanDelete { get; set; }

        public string NoDelReason { get; set; }

        public bool HasDetail { get; set; }
    }
}
