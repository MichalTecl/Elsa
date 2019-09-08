using System;
using Elsa.Common.Noml.Forms;
using Elsa.Core.Entities.Commerce.Accounting;

namespace Elsa.Apps.InvoiceForms.UiForms
{
    public class InvoiceFormRendererFactory : IInvoiceFormRendererFactory
    {
        public Form GetRenderer(IInvoiceForm form)
        {
            var rendererName = form.FormType.GeneratorName;

            if (rendererName.Equals("ReceivingInvoice", StringComparison.InvariantCultureIgnoreCase))
            {
                return new ReceivingInvoiceFormRenderer(form);
            }
            else if (rendererName.Equals("ReleasingForm", StringComparison.InvariantCultureIgnoreCase))
            {
                return new ReleasingInvoiceFormRenderer(form);
            }

            throw new InvalidOperationException($"Cannot find renderer by InvoiceFormType.GeneratorName = '{rendererName}'");
        }
    }
}
