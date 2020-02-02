using System;
using System.Globalization;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Noml.Forms;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.Apps.InvoiceForms.UiForms
{
    public class InvoiceFormRendererFactory : IInvoiceFormRendererFactory
    {
        private readonly CultureInfo m_culture;

        public InvoiceFormRendererFactory(ISession session)
        {
            m_culture = CultureInfo.GetCultureInfo(session.Culture);
        }

        public Form GetRenderer(IInvoiceForm form)
        {
            var rendererName = form.FormType.GeneratorName;

            if (rendererName.Equals("ReceivingInvoice", StringComparison.InvariantCultureIgnoreCase))
            {
                return new ReceivingInvoiceFormRenderer(form, m_culture);
            }
            else if (rendererName.Equals("ReleasingForm", StringComparison.InvariantCultureIgnoreCase))
            {
                return new ReleasingInvoiceFormRenderer(form, m_culture);
            }

            throw new InvalidOperationException($"Cannot find renderer by InvoiceFormType.GeneratorName = '{rendererName}'");
        }
    }
}
