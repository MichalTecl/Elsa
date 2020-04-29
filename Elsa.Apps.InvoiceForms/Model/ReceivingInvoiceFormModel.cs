using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.InvoiceForms.Model
{
    public class ReceivingInvoiceFormModel : InvoiceFormModelBase
    {
        public string Supplier { get; set; }
        
        public string InvoiceVarSymbol { get; set; }
        
        public override TXlsModel ToExcelModel<TXlsModel>() 
        {
            return new ReceivingFormXlsModel
            {
                Date = IssueDate,
                Number = InvoiceFormNumber,
                TargetInventory = InventoryName,
                VariableSymbol = InvoiceVarSymbol,
                Supplier = Supplier,
                Price = PrimaryCurrencyPriceWithoutVat,
                ForeignCurrencyPrice = OriginalCurrencyPriceValue,
                Currency = OriginalCurrencySymbol,
                ForeignCurrencyConversionRate = ConversionRateValue,
                Text = Explanation,
                Link = DownloadUrl
            } as TXlsModel;
        }
    }

    public class ReceivingFormXlsModel
    {
        [XlsColumn("A", "Datum")]
        public string Date { get; set; }

        [XlsColumn("B", "Číslo")]
        public string Number { get; set; }

        [XlsColumn("C", "Na sklad")]
        public string TargetInventory { get; set; }

        [XlsColumn("D", "Var. Symbol")]
        public string VariableSymbol { get; set; }

        [XlsColumn("E", "Dodavatel")]
        public string Supplier { get; set; }

        [XlsColumn("F", "Cena CZK")]
        public decimal Price { get; set; }

        [XlsColumn("G", "Cena v CM")]
        public decimal? ForeignCurrencyPrice { get; set; }

        [XlsColumn("H", "Měna")]
        public string Currency { get; set; }

        [XlsColumn("I", "Kurz")]
        public decimal? ForeignCurrencyConversionRate { get; set; }

        [XlsColumn("J", "Text")]
        public string Text { get; set; }
        
        [XlsColumn("K", "Link")]
        public string Link { get; set; }
    }
}
