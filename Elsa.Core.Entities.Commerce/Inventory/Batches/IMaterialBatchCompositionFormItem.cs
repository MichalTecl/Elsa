using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.Batches
{
    [Entity]
    public interface IMaterialBatchCompositionFormItem : IIntIdEntity
    {
        int MaterialBatchCompositionId { get; set; }
        IMaterialBatchComposition MaterialBatchComposition { get; }

        int InvoiceFormItemId { get; set; }
        IInvoiceFormItem InvoiceFormItem { get; }
    }
}
