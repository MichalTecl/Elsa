using System;

using Elsa.App.Commerce.Preview.Model;
using Elsa.Apps.Common.ViewModels;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.App.Commerce.Preview
{
    [Controller("commerceOverviews")]
    public class PreviewController : ElsaControllerBase
    {
        private readonly IPurchaseOrderRepository m_purchaseOrderRepository;
        private readonly IOrderStatusTranslator m_statusTranslator;
        private readonly OverviewsConfig m_config;

        public PreviewController(IWebSession webSession, ILog log, IPurchaseOrderRepository purchaseOrderRepository, IOrderStatusTranslator statusTranslator, OverviewsConfig config)
            : base(webSession, log)
        {
            m_purchaseOrderRepository = purchaseOrderRepository;
            m_statusTranslator = statusTranslator;
            m_config = config;
        }

        [DoNotLog]
        public ReportTableViewModel GetOrdersOverview()
        {
            var report = new ReportTableViewModel();

            foreach (var row in m_purchaseOrderRepository.GetOrdersOverview(DateTime.Now.AddMonths(-1), DateTime.Now))
            {
                report[row.ErpName, "ERP"] = row.ErpName;
                report[row.ErpName, m_statusTranslator.Translate(row.StatusId)] = row.Count.ToString();
            }

            return report;
        }

        [DoNotLog]
        public MissingPaymentsResult GetMissingPaymentsCount()
        {
            return new MissingPaymentsResult(
                       m_config.MissingPaymentDaysTolerance,
                       m_purchaseOrderRepository.GetMissingPaymentsCount(m_config.MissingPaymentDaysTolerance));
        }
    }
}
