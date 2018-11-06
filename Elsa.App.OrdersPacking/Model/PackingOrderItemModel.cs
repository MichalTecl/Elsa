using System.Collections.Generic;

using Elsa.Commerce.Core.Model;

namespace Elsa.App.OrdersPacking.Model
{
    public class PackingOrderItemModel
    {
        public long ItemId { get; set; }

        public string ProductName { get; set; }

        public string Quantity { get; set; }

        public List<KitItemsCollection> KitItems { get; } = new List<KitItemsCollection>();

        public List<BatchAssignmentViewModel> BatchAssignment { get; } = new List<BatchAssignmentViewModel>();

        public decimal NumericQuantity { get; set; }
    }
}
