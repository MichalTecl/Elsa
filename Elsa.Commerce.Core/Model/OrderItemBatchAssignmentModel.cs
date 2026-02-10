using System;

namespace Elsa.Commerce.Core.Model
{
    public class OrderItemBatchAssignmentModel
    {
        public long OrderItemId { get; set; }

        public int MaterialBatchId { get; set; }

        public decimal AssignedQuantity { get; set; }

        public string BatchNumber { get; set; }

        public string WarningMessage { get; set; }
        public string PinnedWarningMessage { get; set; }
        
        public void Add(OrderItemBatchAssignmentModel assignment)
        {
            if (!assignment.BatchNumber.Equals(BatchNumber, StringComparison.InvariantCultureIgnoreCase) ||
                assignment.OrderItemId != OrderItemId)
            {
                throw new InvalidOperationException("Attempt to add incompatible assignment");
            }

            AssignedQuantity += assignment.AssignedQuantity;
        }
    }
}
