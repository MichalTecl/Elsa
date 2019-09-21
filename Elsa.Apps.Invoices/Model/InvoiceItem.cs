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
        [ListValidation("DATA_Materials!A:A", AllowBlank = false, Error = "Zadejte platný symbol jednotky", ErrorTitle = "!")]
        public string MaterialName { get; set; }

        [XlsColumn("D", "Množství")]
        public decimal Quantity { get; set; }

        [XlsColumn("E", "Jednotka")]
        //[ListValidation("DATA_Units!A:A", AllowBlank = false, Error = "Zadejte platný symbol jednotky", ErrorTitle = "!")]
        [R1C1Formula("=VLOOKUP(R[0]C[-2], DATA_Materials!A1:B9999, 2, FALSE)")]
        public string Unit { get; set; }

        [XlsColumn("F", "Cena bez DPH")]
        public decimal Price { get; set; }
    }
}
