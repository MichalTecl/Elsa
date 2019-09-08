using Elsa.Common.Noml.Forms;
using Elsa.Core.Entities.Commerce.Accounting;

namespace Elsa.Apps.InvoiceForms.UiForms
{
    public interface IInvoiceFormRendererFactory
    {
        Form GetRenderer(IInvoiceForm form);
    }
}
