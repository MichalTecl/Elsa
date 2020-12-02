using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IOrdersFacade
    {
        IPurchaseOrder SetOrderPaid(long orderId, long? paymentId);

        IPurchaseOrder SetOrderSent(long orderId);

        void SetOrderSentAsync(long orderId);

        IEnumerable<IPurchaseOrder> GetAndSyncPaidOrders(DateTime historyDepth, bool skipErp = false);

        IEnumerable<IOrderItem> GetAllConcreteOrderItems(IPurchaseOrder order);

        IEnumerable<Tuple<IPurchaseOrder, decimal>> GetOrdersByUsedBatch(BatchKey batch, int pageSize, int pageNumber);

        IPurchaseOrder ResolveSingleItemKitSelection(IPurchaseOrder entity);
    }
}
