using System.Collections.Generic;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderItemBatchAssignmentChangeRequest
    {
        public long OrderId { get; set; }

        public long OrderItemId { get; set; }

        public List<OrderItemBatchAssignmentDeltaModel> Changes { get; set; }
    }

    public class OrderItemBatchAssignmentDeltaModel
    {
        public string BatchNumber { get; set; }

        public decimal Delta { get; set; }
    }
}
