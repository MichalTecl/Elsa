using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RoboApi;
using XlsSerializer.Core;

namespace Elsa.App.CommonReports
{
    [Controller("CommonReports")]
    public class CommonReportsController : ElsaControllerBase
    {
        private readonly IStockReportLoader m_stockReportLoader;

        public CommonReportsController(IWebSession webSession, ILog log, IStockReportLoader stockReportLoader) : base(webSession, log)
        {
            m_stockReportLoader = stockReportLoader;
        }

        public FileResult GetStockReport(int year, int month, int day)
        {
            var rd = new DateTime(year, month, day).AddHours(23).AddMinutes(59).AddSeconds(59);

            var stockReport = m_stockReportLoader.LoadStockReport(rd);

            var bytes = XlsxSerializer.Instance.Serialize(stockReport);

            return new FileResult($"Stav skladu {StringUtil.FormatDate(rd)}.xlsx", bytes);
        }

        public FileResult GetBatchPricesReport()
        {
            var priceReport = m_stockReportLoader.LoadPriceComponentsReport();

            var bytes = XlsxSerializer.Instance.Serialize(priceReport);

            return new FileResult("Výpočty cen.xlsx", bytes);
        }
    }
}
