using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Accounting;

namespace Elsa.Apps.InvoiceForms.Model
{
    public class GenerationInfoModel
    {
        public GenerationInfoModel(IInvoiceFormGenerationLog log)
        {
            Id = log.Id;
            TimeInfo = log.EventDt.ToLongTimeString();
            IsError = log.IsError;
            IsWarning = log.IsWarning;
            Message = log.Message;
            CanApprove = log.IsWarning && (log.ApproveDt == null);
        }

        public int Id { get; }

        public string TimeInfo { get; }

        public bool IsError { get; }

        public bool IsWarning { get; }

        public bool CanApprove { get; }

        public string Message { get; }

        public List<int> GroupedRecords { get; } = new List<int>();
    }
}
