using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IVirtualProductRepository : ICanPostponeCacheRemovals<IDisposableVirtualProductsRepository>
    {
        IEnumerable<IVirtualProduct> GetVirtualProductsByOrderItem(IPurchaseOrder order, IOrderItem item);

        IEnumerable<IVirtualProduct> GetVirtualProducts(int? erpId, string erpProductId, string placedName);

        void Map(int? erpId, string erpProductId, string placedName, int virtualProductId);

        IEnumerable<IVirtualProductMappableItem> GetMappableItems();

        void Unmap(int? erpId, string erpProductId, string placedName, int virtualProductId);

        IEnumerable<IVirtualProduct> GetAllVirtualProducts();

        IVirtualProduct GetVirtualProductById(int id);

        IVirtualProduct CreateOrUpdateVirtualProduct(int? virtualProductId, string name);

        void CleanCache();

        void DeleteVirtualProduct(int vpId);

        List<ErpProductMapping> ExportErpProductMappings();

        int ImportErpProductMappings(List<ErpProductMapping> mappings);

        List<KitProductXlsModel> ExportKits();
        int ImportKits(List<KitProductXlsModel> mappings);
    }

    public interface IDisposableVirtualProductsRepository : IVirtualProductRepository, IDisposable
    {
    }
}
