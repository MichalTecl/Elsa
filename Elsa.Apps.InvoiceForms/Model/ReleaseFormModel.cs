using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.InvoiceForms.Model
{
    public class ReleaseFormModel : InvoiceFormModelBase
    {
        public string Text { get; set; }

        public string InvoiceVarSymbol { get; set; }

        public override TXlsModel ToExcelModel<TXlsModel>()
        {
            return new ReleaseFormXlsModel
            {
                Date = IssueDate,
                Number = InvoiceFormNumber,
                Text = Text,
                SourceInventory = InventoryName,
                Price = PrimaryCurrencyPriceWithoutVat,
                Link = DownloadUrl,
                Description = Explanation
            } as TXlsModel;
        }
    }

    public class ReleaseFormXlsModel
    {
        [XlsColumn("A", "Datum")]
        public string Date { get; set; }

        [XlsColumn("B", "Číslo")]
        public string Number { get; set; }

        [XlsColumn("C", "Text")]
        public string Text { get; set; }

        [XlsColumn("D", "Ze skladu")]
        public string SourceInventory { get; set; }

        [XlsColumn("E", "Cena CZK")]
        public decimal Price { get; set; }

        [XlsColumn("F", "Link")]
        public string Link { get; set; }

        [XlsColumn("G", "Popis")]
        public string Description { get; set; }
    }
}
