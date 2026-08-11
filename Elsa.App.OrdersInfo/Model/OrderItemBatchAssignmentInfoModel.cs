using System;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderItemBatchAssignmentInfoModel
    {
        public long AssignmentId { get; set; }

        public int MaterialBatchId { get; set; }

        public string BatchNumber { get; set; }

        public decimal Quantity { get; set; }

        public DateTime AssignmentDt { get; set; }

        public string AssignedBy { get; set; }
    }
}
