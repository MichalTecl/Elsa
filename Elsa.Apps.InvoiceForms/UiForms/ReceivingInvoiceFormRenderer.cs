using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Noml.Core;
using Elsa.Common.Noml.Forms;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;

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
            yield return RenderTitle();
            yield return RenderHead();
            yield return RenderItems();
        }
        
        private IRenderable RenderTitle()
        {
            return Row(Col(null, H1("Příjemka na sklad")));
        }

        private IRenderable RenderHead()
        {
            return Frame(FrameStyle.All, Row(RenderLeftTopHead(), RenderSupplierInfo()));
        }

        private Column RenderSupplierInfo()
        {
            var table = NewTable().Row(B("Dodavatel:"));

            if (m_form.Supplier != null)
            {
                var sup = m_form.Supplier;
                table
                    .Row(sup.Name)
                    .Row(sup.Street)
                    .Row(string.Join(" ", sup.Zip, sup.City))
                    .Row($"IČ: {sup.IdentificationNumber}")
                    .Row($"DIČ: {sup.TaxIdentificationNumber}");
            }

            return Col50(Frame(FrameStyle.Left, table));
        }

        private Column RenderLeftTopHead()
        {
            return Col50(
                NewTable()
                    .Row(B("PŘÍJEMKA NA SKLAD č.:"), m_form.InvoiceFormNumber)
                    .Row(B("Vystaveno:"), StringUtil.FormatDate(m_form.IssueDate))
                    .Row(B("Faktura - V.S.:"), m_form.InvoiceVarSymbol)
                );
        }

        private IRenderable RenderItems()
        {
            return Frame(FrameStyle.All,
                NewTable().Head("Položka", "Množství", "Cena bez DPH").Rows(m_form.Items,
                    i => { return new object[]
                    {
                        i.MaterialName,
                        $"{StringUtil.FormatDecimal(i.Quantity)} {i.Unit.Symbol}",
                        StringUtil.FormatDecimal(i.SourceCurrencyPrice ?? i.PrimaryCurrencyPrice)
                    }; }));
        }
        
    }
}
