using System.Collections.Generic;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderItemInfoModel
    {
        public long ItemId { get; set; }

        public string PlacedName { get; set; }

        public decimal Quantity { get; set; }

        public decimal PriceWithVat { get; set; }

        public List<OrderItemInfoModel> Children { get; } = new List<OrderItemInfoModel>();

        public List<OrderItemBatchAssignmentInfoModel> BatchAssignments { get; } = new List<OrderItemBatchAssignmentInfoModel>();
    }
}
