using System.Collections.Generic;
using System.Linq;
using Elsa.Common.Noml.Core;
using Elsa.Common.Noml.Forms;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;

namespace Elsa.Apps.InvoiceForms.UiForms
{
    internal class ReleasingInvoiceFormRenderer : Form
    {
        private readonly IInvoiceForm m_form;

        public ReleasingInvoiceFormRenderer(IInvoiceForm form)
        {
            m_form = form;
        }

        protected override IEnumerable<IRenderable> Build()
        {
            //Title
            yield return Div(Class("captionCont"), H1($"Výdejka ze skladu \"{m_form.MaterialInventory.Name}\""));

            yield return Crlf();
            yield return Frame(FrameStyle.All, Div(RenderHead()));
            yield return RenderItems();
        }
        

        private IEnumerable<IRenderable> RenderHead()
        {
            yield return Div(Class("captionCont"), B($"Výdejka č. {m_form.InvoiceFormNumber}"));

            yield return TitleValue("Vystaveno:", StringUtil.FormatDate(m_form.IssueDate));
            yield return TitleValue("Faktura - V.S.:", m_form.InvoiceVarSymbol);
            yield return TitleValue("Účel:", m_form.Explanation);
        }
        
        private IRenderable RenderItems()
        {
            return Frame(FrameStyle.All, Div(
                NewTable().Head("Položka", "Množství", "Cena bez DPH").Rows(m_form.Items,
                    i =>
                    {
                        var batchesString = string.Join(",", i.Batches.Select(b => b.MaterialBatch?.BatchNumber));

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
