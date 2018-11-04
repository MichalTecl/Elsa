using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IVirtualProductFacade
    {
        IVirtualProduct ProcessVirtualProductEditRequest(int? virtualProductId, string name, string[] materialEntries);

        MaterialAmountModel GetOrderItemMaterialForSingleUnit(IPurchaseOrder order, IOrderItem item);
    }
}
