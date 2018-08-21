using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.App.Commerce.Payments.Models;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;

using Robowire.RoboApi;

namespace Elsa.App.Commerce.Payments
{
    [Controller("paymentPairing")]
    public class PaymentsPairingController : ElsaControllerBase
    {
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IPaymentRepository m_paymentRepository;

        public PaymentsPairingController(IWebSession webSession, IPurchaseOrderRepository orderRepository, IPaymentRepository paymentRepository)
            : base(webSession)
        {
            m_orderRepository = orderRepository;
            m_paymentRepository = paymentRepository;
        }

        public IEnumerable<SuggestedPairModel> GetUnpaidOrders()
        {
            var orders =
                m_orderRepository.GetOrdersByStatus(OrderStatus.PendingPayment)
                    .Where(o => !o.IsPayOnDelivery)
                    .OrderBy(o => o.PurchaseDate)
                    .ToList();

            if (!orders.Any())
            {
                yield break;
            }

            var payments = m_paymentRepository.GetPayments(
                orders.Min(o => o.PurchaseDate).AddDays(-1),
                DateTime.Now.AddDays(1)).ToList();


            foreach (var order in orders)
            {
                var eligiblePayments = payments.Where(p => p.PaymentDt.Date >= order.PurchaseDate.Date).ToList();

                var payment =
                    eligiblePayments.FirstOrDefault(
                        p => p.VariableSymbol.Equals(order.VarSymbol, StringComparison.InvariantCultureIgnoreCase))
                    ?? GetBestMatch(order, eligiblePayments);

                yield return new SuggestedPairModel(new OrderViewModel(order), payment == null ? null : new PaymentViewModel(payment));
            }
        }

        private IPayment GetBestMatch(IPurchaseOrder order, IEnumerable<IPayment> payments)
        {
            var tokens = GetTokensWithWeights(order).Where(t => !string.IsNullOrWhiteSpace(t.Item1)).ToList();

            tokens = tokens.OrderByDescending(t => t.Item2).ToList();

            var distinctTokens = new List<Tuple<string, int>>(tokens.Count);

            foreach (var t in tokens)
            {
                if (distinctTokens.Any(dt => dt.Item1.Equals(t.Item1, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                distinctTokens.Add(t);
            }

            tokens = distinctTokens;

            IPayment found = null;
            var rank = int.MinValue;

            foreach (var payment in payments)
            {
                var localRank = GetRank(payment.SearchText, tokens);
                if (localRank > rank)
                {
                    found = payment;
                    rank = localRank;
                }
            }

            return rank < 2 ? null : found;
        }

        private int GetRank(string target, List<Tuple<string, int>> tokens)
        {
            var result = 0;

            foreach (var token in tokens)
            {
                if (target.Contains(token.Item1))
                {
                    result += token.Item2;
                }
            }

            return result;
        }

        private static IEnumerable<Tuple<string, int>> GetTokensWithWeights(IPurchaseOrder po)
        {
            yield return new Tuple<string, int>(TryTokenize(po.VarSymbol), 10);
            yield return new Tuple<string, int>(TryTokenize(po.CustomerName), 9);
            yield return new Tuple<string, int>(TryTokenize(po.CustomerEmail), 9);

            yield return new Tuple<string, int>(TryTokenize(po.InvoiceAddress.CompanyName), 3);
            yield return new Tuple<string, int>(TryTokenize(po.InvoiceAddress.FirstName), 1);
            yield return new Tuple<string, int>(TryTokenize(po.InvoiceAddress.LastName), 5);
            yield return new Tuple<string, int>(TryTokenize(po.InvoiceAddress.Phone), 1);

            yield return new Tuple<string, int>(TryTokenize(po.DeliveryAddress?.CompanyName), 2);
            yield return new Tuple<string, int>(TryTokenize(po.DeliveryAddress?.FirstName), 1);
            yield return new Tuple<string, int>(TryTokenize(po.DeliveryAddress?.LastName), 4);
            yield return new Tuple<string, int>(TryTokenize(po.DeliveryAddress?.Phone), 3);
        }

        private static string TryTokenize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return null;
            }

            return StringUtil.NormalizeSearchText(100, new [] { s });
        }

    }
}
