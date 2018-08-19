using System;
using System.Linq;

using Elsa.Commerce.Core;
using Elsa.Integration.PaymentSystems.Common;
using Elsa.Jobs.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Jobs.PaymentPairing
{
    public class PairPaymentsJob : IExecutableJob
    {
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IPaymentSystemClientFactory m_clientFactory;
        private readonly IPaymentRepository m_paymentRepository;
        private readonly IDatabase m_database;
        private readonly IOrdersFacade m_orders;

        public PairPaymentsJob(
            IPurchaseOrderRepository orderRepository,
            IPaymentSystemClientFactory clientFactory,
            IPaymentRepository paymentRepository,
            IDatabase database, IOrdersFacade orders)
        {
            m_orderRepository = orderRepository;
            m_clientFactory = clientFactory;
            m_paymentRepository = paymentRepository;
            m_database = database;
            m_orders = orders;
        }

        public void Run(string customDataJson)
        {
            Console.WriteLine("Zacinam parovani...");
            DownloadPayments();

            Console.WriteLine("Hledam objednavky cekajici na platbu");
            var ordersToBePaired =
                m_orderRepository.GetOrdersByStatus(OrderStatus.PendingPayment).Where(o => !o.IsPayOnDelivery).ToList();
            Console.WriteLine($"Nalezeno {ordersToBePaired.Count} zaznamu");

            if (ordersToBePaired.Count == 0)
            {
                Console.WriteLine("Zadne objednavky k parovani");
                return;
            }

            var minDt = ordersToBePaired.Min(o => o.PurchaseDate);
            
            m_paymentRepository.PreloadCache(minDt.AddDays(-1), DateTime.Now);
            
            foreach (var order in ordersToBePaired)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(order.VarSymbol))
                    {
                        throw new InvalidOperationException($"Objednavka {order.OrderNumber} ze systemu {order.Erp?.Description} nema variabilni symbol");
                    }

                    Console.WriteLine($"Hledam platbu k objednavce {order.OrderNumber} podle VS={order.VarSymbol}");

                    var payments = m_paymentRepository.GetPaymentsByVarSymb(order.VarSymbol).Where(p => p.Orders == null || !p.Orders.Any()).ToList();
                    if (!payments.Any())
                    {
                        Console.WriteLine("Platba nenalezena");
                        continue;
                    }

                    if (payments.Count > 1)
                    {
                        Console.WriteLine($"Existuje vice nez jedna platba VS={order.VarSymbol}. Nelze parovat.");
                        continue;
                    }

                    var payment = payments.First();

                    Console.WriteLine("Platba nalezena, paruji...");

                    //TODO currencies
                    if (Math.Abs(payment.Value - order.PriceWithVat) > 0.01m)
                    {
                        Console.WriteLine("Neshoduje se castka, nelze parovat");
                        continue;
                    }

                    m_orders.SetOrderPaid(order.Id, payment.Id);

                    Console.WriteLine("Sparovano");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba: {ex}");
                }
            }
        }

        private void DownloadPayments()
        {
            var lastPayments = m_paymentRepository.GetLastPaymentDates().ToDictionary(p => p.PaymentSourceId, p => p.LastPaymentDt);

            var paymentSources = m_clientFactory.GetAllPaymentSystemClients().ToList();
            foreach (var paymentSource in paymentSources)
            {
                DateTime startDt;
                if (!lastPayments.TryGetValue(paymentSource.Entity.Id, out startDt))
                {
                    startDt = DateTime.MinValue;
                }

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
                
                m_paymentRepository.PreloadCache(startDt, endDt);

                Console.WriteLine($"Stahuji platby ze zdroje {paymentSource.Entity.Description} {startDt} - {endDt}");

                var payments = paymentSource.GetPayments(startDt, endDt).ToList();

                Console.WriteLine($"Stazeno {payments.Count} zaznamu");

                using (var trx = m_database.OpenTransaction())
                {
                    foreach (var payment in payments)
                    {
                        m_paymentRepository.SavePayment(payment);
                    }

                    trx.Commit();
                }
                
                Console.WriteLine("Ulozeno");

                startDt = endDt;
            }
            
        }
    }
}
