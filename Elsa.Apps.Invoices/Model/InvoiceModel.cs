using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.Invoices.Model
{
    [XlsSheet(0, "Faktura")]
    public class InvoiceModel
    {
        #region Invoice Head
        //[XlsCell(0, 0)]
        //[CellStyle(FontStyle = FontStyle.Bold, Locked = true)]
        //public string IdLabel { get; } = "ID";
        //[XlsCell(0, 1)]
        //[CellStyle(FontStyle = FontStyle.Italic, Locked = true)]
        //public int Id { get; set; }

        [XlsCell(1, 0)]
        [CellStyle(FontStyle = FontStyle.Bold, Locked = true)]
        public string InvoiceNumberLabel { get; } = "Č. Faktury";
        [XlsCell(1, 1)]
        public string InvoiceNumber { get; set; }

        [XlsCell(2, 0)]
        [CellStyle(FontStyle = FontStyle.Bold, Locked = true)]
        public string VarSymbolLabel { get; } = "V.S.";
        [XlsCell(2, 1)]
        [R1C1Formula("R[-1]C[0]")]
        public string VarSymbol { get; set; }

        [XlsCell(3, 0)]
        [CellStyle(FontStyle = FontStyle.Bold, Locked = true)]
        public string DateLabel { get; } = "Datum";
        [XlsCell(3, 1, "dd.mm.yyyy")]
        public string Date { get; set; }

        [XlsCell(4, 0)]
        [CellStyle(FontStyle = FontStyle.Bold, Locked = true)]
        public string SupplierLabel { get; } = "Dodavatel";
        [XlsCell(4, 1)]
        [ListValidation("DATA_Suppliers!A:A", AllowBlank = false, Error = "Neznamy dodavatel")]
        public string SupplierName { get; set; }

        [XlsCell(5, 0)]
        [CellStyle(FontStyle = FontStyle.Bold, Locked = true)]
        public string TotalPriceLabel { get; } = "Celk. cena";
        [XlsCell(5, 1)]
        public decimal TotalPrice { get; set; }

        [XlsCell(5, 2)]
        [Formula("=VLOOKUP(B5, DATA_Suppliers!A1:B9999, 2, FALSE)")]
        public string Currency { get; set; }
        #endregion

        #region Items
        [XlsCell(7, 0)]
        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        #endregion

        #region Reference Data
        [XlsSheet(2, "DATA_Suppliers")]
        public List<SupplierAndCurrency> Suppliers { get;  } = new List<SupplierAndCurrency>();

        [XlsSheet(3, "DATA_Materials")]
        public List<MaterialAndUnit> Materials { get; } = new List<MaterialAndUnit>();

        [XlsSheet(4, "DATA_Currencies")]
        public List<string> Currencies { get; } = new List<string>();

        [XlsSheet(5, "DATA_Units")]
        public List<string> Units { get; } = new List<string>();
        
        #endregion
    }
}
