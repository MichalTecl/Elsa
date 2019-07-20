using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges
{
    [Entity]
    public interface IOrderItemInvoiceFormItem : IInvoiceFormItemBridgeBase
    {
        IOrderItemMaterialBatch BatchAssignment { get; }
        long BatchAssignmentId { get; set; }
    }
}
