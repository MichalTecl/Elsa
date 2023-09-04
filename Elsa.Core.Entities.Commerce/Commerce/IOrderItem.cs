using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IOrderItem
    {
        long Id { get; }
        long? PurchaseOrderId { get; set; }
        int? ProductId { get; set; }

        [NotFk]
        [NVarchar(255, true)]
        string ErpOrderItemId { get; set; }

        [NVarchar(255, false)]
        string PlacedName { get; set; }
        decimal Quantity { get; set; }
        decimal TaxedPrice { get; set; }
        decimal TaxPercent { get; set; }
        decimal? Weight { get; set; }

        [NotFk]
        [NVarchar(255, true)]
        string ErpProductId { get; set; }

        IPurchaseOrder PurchaseOrder { get; }

        IProduct Product { get; }

        long? KitParentId { get; set; }

        IOrderItem KitParent { get; }

        [ForeignKey(nameof(IOrderItem.KitParentId))]
        IEnumerable<IOrderItem> KitChildren { get; }

        int? KitItemIndex { get; set; }
        
        [ForeignKey(nameof(IOrderItemMaterialBatch.OrderItemId))]
        IEnumerable<IOrderItemMaterialBatch> AssignedBatches { get; }

        [NVarchar(255, true)]
        string ErpWarehouseItemCode { get; set; }

        [NotFk]
        [NVarchar(255, true)]
        string ErpWarehouseItemId { get; set; }
    }
}
