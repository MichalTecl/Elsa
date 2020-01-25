using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.App.CommonReports.Model
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class InventorySummaryValueItemModel
    {
        [XlsColumn("A", "Sklad", "@")]
        public string InventoryName { get; set; }

        [XlsColumn("B", "Hodnota CZK", "0.00")]
        public decimal Value { get; set; }
    }
}
