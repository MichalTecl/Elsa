using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Elsa.Common.Noml.Core;
using Elsa.Common.Noml.Forms;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Extensions;

namespace Elsa.Apps.InvoiceForms.UiForms
{
    internal class ReleasingInvoiceFormRenderer : Form
    {
        private readonly IInvoiceForm m_form;
        private readonly CultureInfo m_culture;

        public ReleasingInvoiceFormRenderer(IInvoiceForm form, CultureInfo culture)
        {
            m_form = form;
            m_culture = culture;
        }

        protected override IEnumerable<IRenderable> Build()
        {
            //Title
            yield return Div(Class("captionCont"), H1($"Výdejka ze skladu \"{m_form.MaterialInventory.Name}\""));

            yield return Crlf();
            yield return Frame(FrameStyle.All, Div(RenderHead()));
            yield return RenderItems();

            yield return RenderSum();
        }

        private IRenderable RenderSum()
        {
            return Frame(FrameStyle.Top, Div(Crlf(), TitleValue("Celkem:", StringUtil.FormatPrice(m_form.Items.Sum(fi => fi.PrimaryCurrencyPrice), m_culture) + " Kč")));
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
                        var batchesString = string.Join(",", i.Batches.Select(b => b.MaterialBatch?.GetUnid()));

                        return new object[]
                        {
                            HtmlLiteral($"{i.MaterialName}&nbsp;<span class=\"note\">({batchesString})</span>"),
                            $"{StringUtil.FormatDecimal(i.Quantity, m_culture)} {i.Unit.Symbol}",
                            StringUtil.FormatPrice(i.SourceCurrencyPrice ?? i.PrimaryCurrencyPrice, m_culture)
                        };
                    })));
        }
        
    }
}
