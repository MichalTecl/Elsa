using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IInvoiceFormItemMaterialBatch
    {
        int Id { get; }

        int InvoiceFormItemId { get; set; }
        IInvoiceFormItem InvoiceFormItem { get; }

        int MaterialBatchId { get; set; }
        IMaterialBatch MaterialBatch { get; }
    }
}
