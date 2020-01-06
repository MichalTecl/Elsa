using XlsSerializer.Core.Attributes;

namespace Elsa.App.CommonReports.Model
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class BatchPriceComponentItemModel
    {
        [XlsColumn("A", "Materiál", "@")]
        public string MaterialName { get; set; }

        [XlsColumn("B", "Šarže", "@")]
        public string BatchIdentifier { get; set; }

        [XlsColumn("C", "Složka ceny", "@")]
        public string Text { get; set; }

        [XlsColumn("D", "Hodnota", "0.00")]
        public decimal Price { get; set; }
    }
}
