using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IOrderItem
    {
        long Id { get; }
        long PurchaseOrderId { get; set; }
        int ProductId { get; set; }

        [NVarchar(255, false)]
        string ErpOrderItemId { get; set; }

        [NVarchar(255, false)]
        string PlacedName { get; set; }
        decimal Quantity { get; set; }
        decimal TaxedPrice { get; set; }
        decimal TaxPercent { get; set; }

        IPurchaseOrder PurchaseOrder { get; }

        IProduct Product { get; }
    }
}
