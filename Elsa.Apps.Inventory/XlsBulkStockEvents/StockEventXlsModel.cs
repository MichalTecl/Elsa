using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.Inventory.XlsBulkStockEvents
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class StockEventXlsModel
    {
        [XlsColumn("A", "Materiál")]
        public string MaterialName { get; set; }

        [XlsColumn("B", "Šarže")]
        public string BatchNumber { get; set; }

        [XlsColumn("C", "Cíl")]
        public string EventName { get; set; }

        [XlsColumn("D", "Poznámka")]
        public string Note { get; set; }
    }
}
