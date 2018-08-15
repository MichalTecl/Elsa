using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IPurchaseOrderRepository
    {
        long ImportErpOrder(IErpOrderModel orderModel);

        IPurchaseOrder TryLoadOrder(string orderNumber);

        void PreloadOrders(DateTime from, DateTime to);

        IEnumerable<OrdersOverviewModel> GetOrdersOverview(DateTime from, DateTime now);
    }
}
