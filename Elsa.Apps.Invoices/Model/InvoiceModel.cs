using System;
using System.Collections.Generic;
using Elsa.Apps.CommonData.ExcelInterop;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.Invoices.Model
{
    [XlsSheet(0, "Faktura")]
    public class InvoiceModel : ElsaExcelModelBase
    {
        #region Invoice Head
        [XlsCell("B1")]
        [Label("Č. Faktury")]
        public string InvoiceNumber { get; set; }

        [XlsCell("B2")]
        [R1C1Formula("R[-1]C[0]")]
        [Label("V.S.")]
        public string VarSymbol { get; set; }
        
        [XlsCell("B3")]
        [Label("Datum")]
        public DateTime Date { get; set; }

        [XlsCell("B4")]
        [Label("Dodavatel")]
        [ListValidation("DATA_Suppliers!A:A", AllowBlank = false, Error = "Neznamy dodavatel")]
        public string SupplierName { get; set; }

        [XlsCell("B5")]
        [Label("Celk. cena")]
        public decimal TotalPrice { get; set; }

        [XlsCell("C5")]
        [Formula("=VLOOKUP(B4, DATA_Suppliers!A1:B9999, 2, FALSE)")]
        [Label("Měna", LabelLocation.Top)]
        public string Currency { get; set; }
        #endregion

        #region Items
        [XlsCell("A7")]
        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        #endregion
    }
}
