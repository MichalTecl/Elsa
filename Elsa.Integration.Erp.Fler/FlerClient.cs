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
        private readonly IOrderStatusMappingRepository m_mappingRepository;

        public FlerClient(FlerClientConfig config, ILog log, IOrderStatusMappingRepository mappingRepository)
        {
            m_config = config;
            m_log = log;
            m_mappingRepository = mappingRepository;
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

        public IEnumerable<IErpOrderModel> LoadPaidOrders(DateTime @from, DateTime to)
        {
            var mappings = m_mappingRepository.GetMappings(Erp.Id);
            
            var orders = m_fler.LoadOrders();

            foreach (var order in orders)
            {
                IErpOrderStatusMapping mapping;
                if (!mappings.TryGetValue(order.State, out mapping))
                {
                    m_log.Error($"Nenalezeno mapovani stavu objednavky pro Fler order.state = \"{order.State}\"");
                    continue;
                }

                if (!OrderStatus.IsPaid(mapping.OrderStatusId))
                {
                    continue;
                }

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

        public void MakeOrderSent(IPurchaseOrder po)
        {
            throw new NotImplementedException("Fler zatim nepodporuje odesilani zasilek :(");
        }

        public IEnumerable<IErpCustomerModel> LoadCustomers()
        {
            yield break;
        }
    }
}
