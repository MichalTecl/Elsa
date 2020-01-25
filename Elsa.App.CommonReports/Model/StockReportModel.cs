using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.App.CommonReports.Model
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class StockReportModel
    {
        [XlsSheet(1, "Stav skladů - Součet")]
        public List<InventorySummaryValueItemModel> Summary { get; set; }

        [XlsSheet(2, "Stav skladů - Podrobné")]
        public List<StockReportItemModel> Details { get; set; }

        [XlsSheet(3, "Výpočty cen")]
        public List<BatchPriceComponentItemModel> Prices { get; set; }

        [XlsSheet(4, "Nepřímé náklady")]
        public List<FixedCostReportItemModel> FixedCosts { get; set; }
    }
}
