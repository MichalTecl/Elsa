using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Core.Entities.Commerce.Extensions
{
    public static class MappedToOrderItemExtensions
    {
        public static bool IsMatch(this IMappedToOrderItem mapped, int? erpId, string erpProductId, string placedName)
        {
            if ((mapped.ErpId != null) && (mapped.ErpId != erpId))
            {
                return false;
            }

            if ((!string.IsNullOrWhiteSpace(mapped.ErpProductId)) && (mapped.ErpProductId != erpProductId))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(mapped.ItemName) && (mapped.ItemName != placedName))
            {
                return false;
            }

            return true;
        }

        public static bool IsMatch(this IMappedToOrderItem mapped, IPurchaseOrder order, IOrderItem orderItem)
        {
            int? erpId;

            if (orderItem.KitParent != null)
            {
                erpId = orderItem.KitParent.PurchaseOrder.ErpId;
            }
            else if (order != null)
            {
                erpId = order.ErpId;
            }
            else
            {
                throw new InvalidOperationException("Cannot obtain ErpId");
            }

            return mapped.IsMatch(erpId, orderItem.ErpProductId, orderItem.PlacedName);
        }
    }
}
