using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IOrdersFacade
    {
        IPurchaseOrder SetOrderPaid(long orderId, long? paymentId);

        IEnumerable<IPurchaseOrder> GetAndSyncPaidOrders(DateTime historyDepth);
    }
}
