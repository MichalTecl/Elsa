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
        private readonly ILog _log;
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly IOrdersFacade _ordersFacade;
        private readonly OrdersSystemConfig _ordersConfig;

        public SetOrdersPaid(ILog log, IPurchaseOrderRepository orderRepository, IOrdersFacade ordersFacade, OrdersSystemConfig cfg)
        {
            _log = log;
            _orderRepository = orderRepository;
            _ordersFacade = ordersFacade;
            _ordersConfig = cfg;
        }

        public void Run(string customDataJson)
        {
            _log.Info("Zacinam zpracovani dobirek a plateb z Pay Pal");

            var toBePaid = new List<IPurchaseOrder>();
            
            AddPayOnDeliveryOrders(toBePaid);
            AddOrdersToBeSetPaidByMapping(toBePaid);
            AddOrdersToBePaidByPaymentMethod(toBePaid);

            foreach (var order in toBePaid.Distinct(new PropEqualityComparer<IPurchaseOrder>(o => o.Id)))
            {
                try
                {
                    var podText = order.IsPayOnDelivery ? "DOBIRKA" : string.Empty;

                    _log.Info(
                        $"Nastavuji objednavku {order.OrderNumber} '{order.ErpStatusName}' {podText} PayMethod='{order.PaymentMethodName}' jako zaplacenou");
                    _ordersFacade.SetOrderPaid(order.Id, null);
                    _log.Info($"Objednavka {order.OrderNumber} '{order.ErpStatusName}' {podText} nastavena OK");
                }
                catch (Exception ex)
                {
                    _log.Error($"Pokus o nastaveni objednavky {order.Erp.Description}:{order.OrderNumber} jako 'Zaplaceno' selhal", ex);
                }
            }
        }

        private void AddPayOnDeliveryOrders(List<IPurchaseOrder> result)
        {
            _log.Info("Hledam dobirky cekajici na nastaveni stavu Zaplaceno");

            //var readyToPackStatusId = OrderStatus.ReadyToPack.Id;
            var pendingPaymentStatusId = OrderStatus.PendingPayment.Id;

            result.AddRange(
                _orderRepository.GetOrders(
                    query =>
                        query.Where(o => o.IsPayOnDelivery)
                            .Where(o => o.OrderStatusId == pendingPaymentStatusId)));

            _log.Info($"Nalezeno {result.Count} dobirek");
        }

        private void AddOrdersToBeSetPaidByMapping(List<IPurchaseOrder> result)
        {
            var toBePaid = _orderRepository.GetOrdersToMarkPaidInErp().ToList();

            _log.Info($"Nalezeno {toBePaid.Count} objednavek, ktere se maji nastavit jako zaplacene na zaklade stavu v ERP");

            result.AddRange(toBePaid);
        }

        private void AddOrdersToBePaidByPaymentMethod(List<IPurchaseOrder> result)
        {
            _log.Info("Zacinam zpracovavat objednavky podle seznamu platebnich metod v OrderProcessing.PaymentMethodsToSetPaidAuto");
            var pendingPaymentStatusId = OrderStatus.PendingPayment.Id;

            foreach (var payMethodName in _ordersConfig.PaymentMethodsToSetPaidAuto ?? new List<string>(0))
            {
                _log.Info($"Hledam nezaplacene objednavky s platebni metodou {payMethodName}:");
                
                var items = 
                    _orderRepository.GetOrders(
                        query =>
                            query.Where(o => o.OrderStatusId == pendingPaymentStatusId)
                            .Where(o => o.PaymentMethodName == payMethodName)).ToList();

                _log.Info($"Nalezeno {items.Count} nezaplacenych objednavek s platebni metodou '{payMethodName}'");
                result.AddRange(items);
            }

            _log.Info("Zpracovani objednavek podle seznamu platebnich metod v OrderProcessing.PaymentMethodsToSetPaidAuto hotovo");
        }
    }
}
