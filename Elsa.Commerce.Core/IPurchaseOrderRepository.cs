﻿using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Commerce;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core
{
    public interface IPurchaseOrderRepository
    {
        long ImportErpOrder(IErpOrderModel orderModel);

        void ImportErpOrders(int erpId, List<IErpOrderModel> orders);

        IPurchaseOrder TryLoadOrderByOrderNumber(string orderNumber);

        void PreloadOrders(DateTime from, DateTime to);

        IEnumerable<OrdersOverviewModel> GetOrdersOverview(DateTime from, DateTime now);

        IEnumerable<IPurchaseOrder> GetOrdersByStatus(IOrderStatus status, DateTime from, DateTime to);

        int CountOrdersToPack();

        IEnumerable<IPurchaseOrder> GetOrdersByStatus(IOrderStatus status);

        IEnumerable<IPurchaseOrder> GetOrders(Action<IQueryBuilder<IPurchaseOrder>> query);
            
        IPurchaseOrder GetOrder(long orderId);

        IEnumerable<IPurchaseOrder> GetOrdersToMarkPaidInErp();

        int GetMissingPaymentsCount(int businessDaysTolerance);

        IEnumerable<IOrderItem> GetChildItemsByParentItemId(long parentItemId);

        IEnumerable<IPurchaseOrder> GetOrdersByMaterialBatch(int batchId);

        void UpdateOrderItemBatch(IOrderItem orderItem, int batchId, decimal quantity);

        IEnumerable<IPurchaseOrder> GetReturns(int month, int year);

        long? SearchOrder(string orderNumberEndsWith, int orderStatusId);
        void SetProcessBlock(IPurchaseOrder order, string stage, string message);

        string TryGetProcessBlockMessage(long orderId, string stage);

        DateTime? GetLastSuccessSyncDt(int erpId);

        int StartSyncSession(int erpId);

        void EndSyncSession(int sessionId);
    }
}
