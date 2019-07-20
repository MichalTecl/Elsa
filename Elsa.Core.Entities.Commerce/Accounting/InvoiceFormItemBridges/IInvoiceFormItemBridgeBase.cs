using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges
{
    public interface IInvoiceFormItemBridgeBase : IIntIdEntity
    {

        IInvoiceFormItem InvoiceFormItem { get; }
        int InvoiceFormItemId { get; set; }
    }
}
