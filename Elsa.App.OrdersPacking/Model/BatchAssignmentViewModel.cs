using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;

namespace Elsa.App.OrdersPacking.Model
{
    public class BatchAssignmentViewModel : OrderItemBatchAssignmentModel
    {
        public BatchAssignmentViewModel(OrderItemBatchAssignmentModel src)
        {
            OrderItemId = src.OrderItemId;
            MaterialBatchId = src.MaterialBatchId;
            AssignedQuantity = src.AssignedQuantity;
            BatchNumber = src.BatchNumber;
            WarningMessage = src.WarningMessage;
        }

        public bool CanSplit { get; set; }

        public bool IsSplit { get; set; }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(BatchNumber);
            }
        }
    }
}
