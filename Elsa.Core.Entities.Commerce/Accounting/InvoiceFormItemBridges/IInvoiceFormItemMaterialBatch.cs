using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges
{
    [Entity]
    public interface IInvoiceFormItemMaterialBatch : IInvoiceFormItemBridgeBase
    {
        int MaterialBatchId { get; set; }
        IMaterialBatch MaterialBatch { get; }
    }
}
