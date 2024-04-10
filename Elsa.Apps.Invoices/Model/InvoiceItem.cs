using Elsa.Apps.CommonData.ExcelInterop;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.Invoices.Model
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class InvoiceItem
    {
        [XlsColumn("A", "Id")]
        [CellStyle(FontStyle = FontStyle.Italic)]
        public int Id { get; set; }

        [XlsColumn("B", "Šarže")]
        public string BatchNumber { get; set; }

        [XlsColumn("C", "Materiál")]
        [ValidateMaterial]
        public string MaterialName { get; set; }

        [XlsColumn("D", "Množství", numberFormat: "0.00")]
        public decimal Quantity { get; set; }

        [XlsColumn("E", "Jednotka")]
        [SetUnitByMaterial("R[0]C[-2]")]
        public string Unit { get; set; }

        [XlsColumn("F", "Cena bez DPH", numberFormat: "0.00")]
        public decimal Price { get; set; }
    }
}
