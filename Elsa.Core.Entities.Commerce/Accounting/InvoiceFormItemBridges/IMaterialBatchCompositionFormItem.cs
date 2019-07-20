using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges
{
    [Entity]
    public interface IMaterialBatchCompositionFormItem : IInvoiceFormItemBridgeBase
    {
        int MaterialBatchCompositionId { get; set; }
        IMaterialBatchComposition MaterialBatchComposition { get; }
    }
}
