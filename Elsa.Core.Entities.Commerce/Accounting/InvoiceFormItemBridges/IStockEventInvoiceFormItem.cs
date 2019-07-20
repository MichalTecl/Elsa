using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges
{
    [Entity]
    public interface IStockEventInvoiceFormItem : IInvoiceFormItemBridgeBase
    {
        int MaterialStockEventId { get; set; }
        IMaterialStockEvent MaterialStockEvent { get; }
    }
}
