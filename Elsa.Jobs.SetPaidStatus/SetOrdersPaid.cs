using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Configuration;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Jobs.Common;

namespace Elsa.Jobs.SetPaidStatus
{
    public class SetOrdersPaid : IExecutableJob
    {
        private readonly ILog m_log;
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IOrdersFacade m_ordersFacade;
        private readonly OrdersSystemConfig m_ordersConfig;

        public SetOrdersPaid(ILog log, IPurchaseOrderRepository orderRepository, IOrdersFacade ordersFacade, OrdersSystemConfig cfg)
        {
            m_log = log;
            m_orderRepository = orderRepository;
            m_ordersFacade = ordersFacade;
            m_ordersConfig = cfg;
        }

        public void Run(string customDataJson)
        {
            m_log.Info("Zacinam zpracovani dobirek a plateb z Pay Pal");

            var toBePaid = new List<IPurchaseOrder>();
            
            AddPayOnDeliveryOrders(toBePaid);
            AddOrdersToBeSetPaidByMapping(toBePaid);
            AddOrdersToBePaidByPaymentMethod(toBePaid);

            foreach (var order in toBePaid.Distinct(new PropEqualityComparer<IPurchaseOrder>(o => o.Id)))
            {
                try
                {
                    var podText = order.IsPayOnDelivery ? "DOBIRKA" : string.Empty;

                    m_log.Info(
                        $"Nastavuji objednavku {order.OrderNumber} '{order.ErpStatusName}' {podText} PayMethod='{order.PaymentMethodName}' jako zaplacenou");
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

        private void AddOrdersToBePaidByPaymentMethod(List<IPurchaseOrder> result)
        {
            m_log.Info("Zacinam zpracovavat objednavky podle seznamu platebnich metod v OrderProcessing.PaymentMethodsToSetPaidAuto");
            var pendingPaymentStatusId = OrderStatus.PendingPayment.Id;

            foreach (var payMethodName in m_ordersConfig.PaymentMethodsToSetPaidAuto ?? new List<string>(0))
            {
                m_log.Info($"Hledam nezaplacene objednavky s platebni metodou {payMethodName}:");
                
                var items = 
                    m_orderRepository.GetOrders(
                        query =>
                            query.Where(o => o.OrderStatusId == pendingPaymentStatusId)
                            .Where(o => o.PaymentMethodName == payMethodName)).ToList();

                m_log.Info($"Nalezeno {items.Count} nezaplacenych objednavek s platebni metodou '{payMethodName}'");
                result.AddRange(items);
            }

            m_log.Info("Zpracovani objednavek podle seznamu platebnich metod v OrderProcessing.PaymentMethodsToSetPaidAuto hotovo");
        }
    }
}
