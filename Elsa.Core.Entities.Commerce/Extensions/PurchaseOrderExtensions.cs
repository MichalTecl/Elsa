using Elsa.Core.Entities.Commerce.Commerce;
using System;
using System.Collections.Generic;

namespace Elsa.Core.Entities.Commerce.Extensions
{
    public static class PurchaseOrderExtensions
    {
        public static Dictionary<string, string> ToDictionary(this IPurchaseOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            return new Dictionary<string, string> { { "OrderNumber", order.OrderNumber } };
        }
    }
}
