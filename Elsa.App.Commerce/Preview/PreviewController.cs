using System;
using System.Collections.Generic;

using Elsa.Apps.Common.ViewModels;
using Elsa.Commerce.Core;
using Elsa.Common;

using Robowire.RoboApi;

namespace Elsa.App.Commerce.Preview
{
    [Controller("commerceOverviews")]
    public class PreviewController : ElsaControllerBase
    {
        private readonly IPurchaseOrderRepository m_purchaseOrderRepository;

        private readonly IOrderStatusTranslator m_statusTranslator;
        

        

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

        public PreviewController(IWebSession webSession, IPurchaseOrderRepository purchaseOrderRepository, IOrderStatusTranslator statusTranslator)
            : base(webSession)
        {
            m_purchaseOrderRepository = purchaseOrderRepository;
            m_statusTranslator = statusTranslator;
        }
    }
}
