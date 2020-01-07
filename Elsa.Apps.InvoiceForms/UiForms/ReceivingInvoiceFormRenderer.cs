using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Noml.Core;
using Elsa.Common.Noml.Forms;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Extensions;

namespace Elsa.Apps.InvoiceForms.UiForms
{
    internal class ReceivingInvoiceFormRenderer : Form
    {
        private readonly IInvoiceForm m_form;

        public ReceivingInvoiceFormRenderer(IInvoiceForm form)
        {
            m_form = form;
        }

        protected override IEnumerable<IRenderable> Build()
        {
            //Title
            yield return Div(Class("captionCont"), H1($"Příjemka na sklad \"{m_form.MaterialInventory.Name}\""));

            yield return Crlf();

            yield return Frame(FrameStyle.All, Div(RenderHead(), RenderSupplierInfo()));

            yield return RenderItems();
        }
        

        private IEnumerable<IRenderable> RenderHead()
        {
            yield return Div(Class("captionCont"), B($"Příjemka č. {m_form.InvoiceFormNumber}"));

            yield return TitleValue("Vystaveno:", StringUtil.FormatDate(m_form.IssueDate));
            yield return TitleValue("Faktura - V.S.:", m_form.InvoiceVarSymbol);
        }

        private IEnumerable<IRenderable> RenderSupplierInfo()
        {
            if (m_form.Supplier == null)
            {
                yield break;
            }

            yield return Crlf();

            yield return Div(Class("captionCont"), B("Dodavatel:"));

            var sup = m_form.Supplier;

            yield return Tabbed(ValueRow(sup.Name),
                ValueRow(sup.Street),
                ValueRow(string.Join(" ", sup.Zip, sup.City)),
                ValueRow($"IČ: {sup.IdentificationNumber}"),
                ValueRow($"DIČ: {sup.TaxIdentificationNumber}"));
        }

        
        private IRenderable RenderItems()
        {
            return Frame(FrameStyle.All, Div(
                NewTable().Head("Položka", "Množství", "Cena bez DPH").Rows(m_form.Items,
                    i =>
                    {
                        var batchesString = string.Join(",", i.Batches.Select(b => b.MaterialBatch?.GetUnid()));

                        return new object[]
                        {
                            HtmlLiteral($"{i.MaterialName}&nbsp;<span class=\"note\">({batchesString})</span>"),
                            $"{StringUtil.FormatDecimal(i.Quantity)} {i.Unit.Symbol}",
                            StringUtil.FormatPrice(i.SourceCurrencyPrice ?? i.PrimaryCurrencyPrice)
                        };
                    })));
        }
        
    }
}
