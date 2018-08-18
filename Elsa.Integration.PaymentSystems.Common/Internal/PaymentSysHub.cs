using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.Integration.PaymentSystems.Common.Internal
{
    internal class PaymentSysHub : IPaymentSystemHub
    {
        private readonly IEnumerable<IPaymentSystemClient> m_clients;

        public PaymentSysHub(IEnumerable<IPaymentSystemClient> clients)
        {
            m_clients = clients;
        }

        public IEnumerable<IPayment> GetPayments(DateTime from, DateTime to)
        {
            foreach (var system in m_clients)
            {
                foreach (var payment in system.GetPayments(from, to))
                {
                    yield return payment;
                }
            }
        }
    }
}
