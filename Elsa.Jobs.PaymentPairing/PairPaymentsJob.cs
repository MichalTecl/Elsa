using System;
using System.Linq;

using Elsa.Commerce.Core;
using Elsa.Common.Logging;
using Elsa.Integration.PaymentSystems.Common;
using Elsa.Jobs.Common;

using RobOrm.Core;

namespace Elsa.Jobs.PaymentPairing
{
    public class PairPaymentsJob : IExecutableJob
    {
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly IPaymentSystemClientFactory _clientFactory;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IDatabase _database;
        private readonly IOrdersFacade _orders;
        private readonly ILog _log;

        public PairPaymentsJob(
            IPurchaseOrderRepository orderRepository,
            IPaymentSystemClientFactory clientFactory,
            IPaymentRepository paymentRepository,
            IDatabase database, IOrdersFacade orders, ILog log)
        {
            _orderRepository = orderRepository;
            _clientFactory = clientFactory;
            _paymentRepository = paymentRepository;
            _database = database;
            _orders = orders;
            _log = log;
        }

        public void Run(string customDataJson)
        {
            _log.Info("Zacinam job \"Parovani objednavek\"");
            DownloadPayments();

            _log.Info("Hledam objednavky cekajici na platbu");
            var ordersToBePaired =
                _orderRepository.GetOrdersByStatus(OrderStatus.PendingPayment).Where(o => !o.IsPayOnDelivery).ToList();
            _log.Info($"Nalezeno {ordersToBePaired.Count} zaznamu");

            if (ordersToBePaired.Count == 0)
            {
                _log.Info("Zadne objednavky k parovani");
                return;
            }

            var minDt = ordersToBePaired.Min(o => o.PurchaseDate);
            
            _paymentRepository.PreloadCache(minDt.AddDays(-1), DateTime.Now);
            
            foreach (var order in ordersToBePaired)
            {
                _log.Info($"Zacinam parovat objednavku OrderNo={order.OrderNumber} OrderId={order.Id}");
                try
                {
                    if (string.IsNullOrWhiteSpace(order.VarSymbol))
                    {
                        throw new InvalidOperationException($"Objednavka {order.OrderNumber} ze systemu {order.Erp?.Description} nema variabilni symbol");
                    }

                    _log.Info($"Hledam platbu k objednavce {order.OrderNumber} podle VS={order.VarSymbol}");

                    var payments = _paymentRepository.GetPaymentsByVarSymb(order.VarSymbol).Where(p => p.Orders == null || !p.Orders.Any()).ToList();
                    if (!payments.Any())
                    {
                        _log.Info("Platba nenalezena");
                        continue;
                    }

                    if (payments.Count > 1)
                    {
                        _log.Info($"Existuje vice nez jedna platba VS={order.VarSymbol}. Nelze parovat.");
                        continue;
                    }

                    var payment = payments.First();

                    _log.Info($"Platba k objednavce {order.OrderNumber} nalezena, paruji...");

                    //TODO currencies
                    if (Math.Abs(payment.Value - order.PriceWithVat) > 0.01m)
                    {
                        _log.Info("Neshoduje se castka, nelze parovat");
                        continue;
                    }

                    _orders.SetOrderPaid(order.Id, payment.Id);

                    _log.Info($"Sparovano: Objednavka OrderNo={order.OrderNumber} OrderId={order.Id}");
                }
                catch (Exception ex)
                {
                    _log.Error("Chyba:", ex);
                }
            }
        }

        private void DownloadPayments()
        {
            _log.Info("Zacinam stahovani plateb");

            var lastPayments = _paymentRepository.GetLastPaymentDates().ToDictionary(p => p.PaymentSourceId, p => p.LastPaymentDt);

            var paymentSources = _clientFactory.GetAllPaymentSystemClients().ToList();
            foreach (var paymentSource in paymentSources)
            {
                DateTime startDt;
                if (!lastPayments.TryGetValue(paymentSource.Entity.Id, out startDt))
                {
                    startDt = DateTime.MinValue;
                }

                _log.Info($"Posledni platba z '{paymentSource.Entity.Description}' je z {startDt}");

                DownloadPayments(paymentSource, startDt);
            }
        }

        private void DownloadPayments(IPaymentSystemClient paymentSource, DateTime startDt)
        {
            if (startDt < paymentSource.CommonSettings.HistoryStart)
            {
                startDt = paymentSource.CommonSettings.HistoryStart;
            }
            
            while (startDt < DateTime.Now.AddDays(1))
            {
                var endDt = startDt.AddDays(paymentSource.CommonSettings.BatchSizeDays);
                
                _paymentRepository.PreloadCache(startDt, endDt);

                _log.Info($"Stahuji platby ze zdroje {paymentSource.Entity.Description} {startDt} - {endDt}");

                var payments = paymentSource.GetPayments(startDt, endDt).ToList();

                _log.Info($"Stazeno {payments.Count} zaznamu");

                using (var trx = _database.OpenTransaction())
                {
                    foreach (var payment in payments)
                    {
                        _paymentRepository.SavePayment(payment);
                    }

                    trx.Commit();
                }
                
                _log.Info("Ulozeno");

                startDt = endDt;
            }
        }
    }
}
