using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core;
using Elsa.Jobs.Common;

namespace Elsa.Jobs.PaymentPairing
{
    public class PairPaymentsJob : IExecutableJob
    {
        private readonly IPurchaseOrderRepository m_orderRepository;

        public PairPaymentsJob(IPurchaseOrderRepository orderRepository)
        {
            m_orderRepository = orderRepository;
        }

        public void Run(string customDataJson)
        {
            var ordersToBePaired = m_orderRepository.GetOrdersByStatus(OrderStatus.PendingPayment).Where(o => !o.);
        }
    }
}
