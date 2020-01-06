using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.App.CommonReports.Model
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class StockReportItemModel
    {
        [XlsColumn("A", "Sklad", "@")]
        public string InventoryName { get; set; }

        [XlsColumn("B", "Materiál", "@")]
        public string MaterialName { get; set; }

        [XlsColumn("C", "Šarže", "@")]
        public string BatchIdentifier { get; set; }

        [XlsColumn("D", "Stav", "0.00")]
        public decimal Amount { get; set; }

        [XlsColumn("E", "Jednotka", "@")]
        public string UnitSymbol { get; set; }

        [XlsColumn("F", "Hodnota CZK", "0.00")]
        public decimal Price { get; set; }
    }
}
