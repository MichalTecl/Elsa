using System;
using System.Linq;

using Elsa.App.Commerce.Preview.Model;
using Elsa.Apps.Common.ViewModels;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
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
        private readonly ICache m_cache;
        private readonly ISession m_session;

        public PreviewController(IWebSession webSession, ILog log, IPurchaseOrderRepository purchaseOrderRepository, IOrderStatusTranslator statusTranslator, OverviewsConfig config, ICache cache)
            : base(webSession, log)
        {
            m_purchaseOrderRepository = purchaseOrderRepository;
            m_statusTranslator = statusTranslator;
            m_config = config;
            m_cache = cache;
            m_session = webSession;
        }

        [DoNotLog]
        public ReportTableViewModel GetOrdersOverview()
        {
            return m_cache.ReadThrough($"ordersOverview_{m_session.Project.Id}", TimeSpan.FromMinutes(10),() => {
                var report = new ReportTableViewModel();

                foreach (var row in m_purchaseOrderRepository.GetOrdersOverview(DateTime.Now.AddMonths(-1),
                    DateTime.Now))
                {
                    report[row.ErpName, "ERP"] = row.ErpName;
                    report[row.ErpName, m_statusTranslator.Translate(row.StatusId)] = row.Count.ToString();
                }

                return report;
            });
        }

        [DoNotLog]
        public MissingPaymentsResult GetMissingPaymentsCount()
        {
            return new MissingPaymentsResult(
                       m_config.MissingPaymentDaysTolerance,
                       m_purchaseOrderRepository.GetMissingPaymentsCount(m_config.MissingPaymentDaysTolerance));
        }

        [DoNotLog]
        public int GetReadyToPackCount()
        {
            return
                m_purchaseOrderRepository.CountOrdersToPack();
        }
    }
}
