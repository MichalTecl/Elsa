using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Jobs.Common;

namespace Elsa.Jobs.SetPaidStatus
{
    public class SetOrdersPaid : IExecutableJob
    {
        private readonly ILog m_log;
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IOrdersFacade m_ordersFacade;

        public SetOrdersPaid(ILog log, IPurchaseOrderRepository orderRepository, IOrdersFacade ordersFacade)
        {
            m_log = log;
            m_orderRepository = orderRepository;
            m_ordersFacade = ordersFacade;
        }

        public void Run(string customDataJson)
        {
            m_log.Info("Zacinam zpracovani dobirek a plateb z Pay Pal");

            var toBePaid = new List<IPurchaseOrder>();

            AddPayOnDeliveryOrders(toBePaid);
            AddOrdersToBeSetPaidByMapping(toBePaid);

            foreach (var order in toBePaid)
            {
                try
                {
                    var podText = order.IsPayOnDelivery ? "DOBIRKA" : string.Empty;

                    m_log.Info(
                        $"Nastavuji objednavku {order.OrderNumber} '{order.ErpStatusName}' {podText} jako zaplacenou");
                    m_ordersFacade.SetOrderPaid(order.Id, null);
                    m_log.Info($"Objednavka {order.OrderNumber} '{order.ErpStatusName}' {podText} nastavena OK");
                }
                catch (Exception ex)
                {
                    m_log.Error($"Pokus o nastaveni objednavky {order.Erp.Description}:{order.OrderNumber} jako 'Zaplaceno' selhal", ex);
                }
            }
        }

        private void AddPayOnDeliveryOrders(List<IPurchaseOrder> result)
        {
            m_log.Info("Hledam dobirky cekajici na nastaveni stavu Zaplaceno");

            //var readyToPackStatusId = OrderStatus.ReadyToPack.Id;
            var pendingPaymentStatusId = OrderStatus.PendingPayment.Id;

            result.AddRange(
                m_orderRepository.GetOrders(
                    query =>
                        query.Where(o => o.IsPayOnDelivery)
                            .Where(o => o.OrderStatusId == pendingPaymentStatusId)));

            m_log.Info($"Nalezeno {result.Count} dobirek");
        }

        private void AddOrdersToBeSetPaidByMapping(List<IPurchaseOrder> result)
        {
            var toBePaid = m_orderRepository.GetOrdersToMarkPaidInErp().ToList();

            m_log.Info($"Nalezeno {toBePaid.Count} objednavek, ktere se maji nastavit jako zaplacene na zaklade stavu v ERP");

            result.AddRange(toBePaid);
        }
    }
}
