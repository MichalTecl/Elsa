using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Commerce.Core
{
    public interface IOrdersFacade
    {
        IPurchaseOrder SetOrderPaid(long orderId, long? paymentId);

        IPurchaseOrder SetOrderCancelled(long orderId);

        IPurchaseOrder SetOrderSent(long orderId);

        void SetOrderSentAsync(long orderId);

        IEnumerable<IPurchaseOrder> GetAndSyncPaidOrders(string shipProvider, bool skipErp = false);

        IEnumerable<IOrderItem> GetAllConcreteOrderItems(IPurchaseOrder order);

        IEnumerable<Tuple<IPurchaseOrder, decimal>> GetOrdersByUsedBatch(BatchKey batch, int pageSize, int pageNumber);

        IPurchaseOrder ResolveSingleItemKitSelection(IPurchaseOrder entity);
        IPurchaseOrder EnsureActualizedOrder(IPurchaseOrder order);

        int ProcessOrderBatch(string processCode, int pageSize, DateTime historyStart, Func<IPurchaseOrder, bool> filter, Action<List<IPurchaseOrder>> process);

        IOrderProcessingLog LogOrderProcess(long orderId, string code, string description, bool throwIfAlreadyExists = true);

        IOrderProcessingLog TryGetProcessingLog(long orderId, string code);

        List<IOrderProcessingLog> GetProcessingLog(long orderId);
    }

    public static class OrderProcessingCodes
    {
        public const string UNPAID_ORDER_MANUAL_CANCEL = "UNPAID_ORDER_MANUAL_CANCEL";
        public const string ORDER_ITEM_BATCH_ASSIGNMENT_CHANGE = "ORDER_ITEM_BATCH_ASSIGNMENT_CHANGE";
    }
}
