using System.Collections.Generic;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderItemInfoModel
    {
        public long OrderId { get; set; }

        public string OrderNumber { get; set; }

        public long ItemId { get; set; }

        public string PlacedName { get; set; }

        public decimal Quantity { get; set; }

        public decimal PriceWithVat { get; set; }

        public bool BatchAssignmentsLocked { get; set; }

        public string BatchAssignmentsLockedReason { get; set; }

        public string BatchAssignmentDateNotice { get; set; }

        public List<OrderItemInfoModel> Children { get; } = new List<OrderItemInfoModel>();

        public List<OrderItemBatchAssignmentInfoModel> BatchAssignments { get; } = new List<OrderItemBatchAssignmentInfoModel>();
    }
}
