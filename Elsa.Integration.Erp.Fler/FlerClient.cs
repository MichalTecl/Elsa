using System;
using System.Collections.Generic;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.Erp.Fler.Model;
using Elsa.Integration.Erp.Flox;

namespace Elsa.Integration.Erp.Fler
{
    public class FlerClient : IErpClient
    {
        private readonly FlerClientConfig m_config;

        private readonly Infrastructure.Fler m_fler;

        public FlerClient(FlerClientConfig config)
        {
            m_config = config;
            m_fler = new Infrastructure.Fler();
            m_fler.LogIn(config.User, config.Password);
        }

        public IErp Erp { get; set; }

        public IErpDataMapper Mapper { get; } = new FlerDataMapper();

        public IErpCommonSettings CommonSettings => m_config;
        
        public IEnumerable<IErpOrderModel> LoadOrders(DateTime @from, DateTime? to = null)
        {
            var orders = m_fler.LoadOrders();

            foreach (var order in orders)
            {
                var detail = m_fler.LoadOrderDetail(order.OrderId.ToString());

                yield return new FlerErpOrder(detail, Erp.Id);
            }
        }

        public void ChangeOrderStatus(string orderId)
        {
            throw new NotImplementedException();
        }
    }
}
