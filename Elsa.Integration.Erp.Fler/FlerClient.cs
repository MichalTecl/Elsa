using System;
using System.Collections.Generic;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.Erp.Fler.Model;
using Elsa.Integration.Erp.Flox;

namespace Elsa.Integration.Erp.Fler
{
    public class FlerClient : IErpClient
    {
        private readonly FlerClientConfig m_config;
        private readonly Infrastructure.Fler m_fler;
        private readonly ILog m_log;

        public FlerClient(FlerClientConfig config, ILog log)
        {
            m_config = config;
            m_log = log;
            m_fler = new Infrastructure.Fler();
            m_fler.LogIn(config.User, config.Password);
        }

        public IErp Erp { get; set; }

        public IErpDataMapper Mapper { get; } = new FlerDataMapper();

        public IErpCommonSettings CommonSettings => m_config;
        
        public IEnumerable<IErpOrderModel> LoadOrders(DateTime from, DateTime? to = null)
        {
            var orders = m_fler.LoadOrders();

            foreach (var order in orders)
            {
                var detail = m_fler.LoadOrderDetail(order.OrderId.ToString());

                yield return new FlerErpOrder(detail, Erp.Id);
            }
        }

        public void MarkOrderPaid(IPurchaseOrder po)
        {
            if (!m_config.EnableWriteOperations)
            {
                m_log.Error($"!!! Fler - MarkOrderPaid({po.OrderNumber}) - neaktivni operace");
                return;
            }

            m_fler.SetOrderPaid(po.ErpOrderId);
        }

        public IErpOrderModel LoadOrder(string orderNumber)
        {
            var detail = m_fler.LoadOrderDetail(orderNumber);
            return new FlerErpOrder(detail, Erp.Id);
        }
    }
}
