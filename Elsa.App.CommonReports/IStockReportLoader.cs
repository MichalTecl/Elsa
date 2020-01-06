using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.App.CommonReports.Model;

namespace Elsa.App.CommonReports
{
    public interface IStockReportLoader
    {
        IEnumerable<StockReportItemModel> LoadStockReport(DateTime forDateTime);

        IList<BatchPriceComponentItemModel> LoadPriceComponentsReport();
    }
}
