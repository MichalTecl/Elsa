using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IOrdersFacade
    {
        IPurchaseOrder SetOrderPaid(long orderId, long? paymentId);

        IPurchaseOrder SetOrderSent(long orderId);

        IEnumerable<IPurchaseOrder> GetAndSyncPaidOrders(DateTime historyDepth);
    }
}
