using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IVirtualProductRepository
    {
        IEnumerable<IVirtualProduct> GetVirtualProductsByOrderItem(IPurchaseOrder order, IOrderItem item);

        IEnumerable<IVirtualProduct> GetVirtualProducts(int? erpId, string erpProductId, string placedName);

        void Map(int? erpId, string erpProductId, string placedName, int virtualProductId);

        IEnumerable<IVirtualProductMappableItem> GetMappableItems();

        void Unmap(int? erpId, string erpProductId, string placedName, int virtualProductId);
    }
}
