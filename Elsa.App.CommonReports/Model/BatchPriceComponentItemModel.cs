using XlsSerializer.Core.Attributes;

namespace Elsa.App.CommonReports.Model
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class BatchPriceComponentItemModel
    {
        [XlsColumn("A", "Založeno", "@")]
        public string Month { get; set; }

        [XlsColumn("B", "Materiál", "@")]
        public string MaterialName { get; set; }

        [XlsColumn("C", "Šarže", "@")]
        public string BatchIdentifier { get; set; }

        [XlsColumn("D", "Složka ceny", "@")]
        public string Text { get; set; }

        [XlsColumn("E", "Hodnota", "0.00")]
        public decimal Price { get; set; }
    }
}
