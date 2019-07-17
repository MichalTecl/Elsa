using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.ProductionSteps
{
    [Entity]
    public interface IBatchStepBatchInvoiceItem : IIntIdEntity
    {
        int BatchProductionStepSourceBatchId { get; set; }
        IBatchProuctionStepSourceBatch BatchProductionStepSourceBatch { get; }

        int InvoiceFormItemId { get; set; }
        IInvoiceFormItem InvoiceFormItem { get; }
    }
}
