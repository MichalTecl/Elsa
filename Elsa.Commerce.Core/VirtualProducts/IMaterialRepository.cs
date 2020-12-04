using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IMaterialRepository : ICanPostponeCacheRemovals<IMaterialRepositoryWithPostponedCache>
    {
        IExtendedMaterialModel GetMaterialById(int materialId);

        IExtendedMaterialModel GetMaterialByName(string materialName);

        IEnumerable<MaterialComponent> GetMaterialsByVirtualProductId(int virtualProductId);

        IEnumerable<IExtendedMaterialModel> GetAllMaterials(int? inventoryId);

        void DetachMaterial(int virtualProductId, int materialId);

        void AddOrUpdateComponent(int vpId, int materialId, decimal requestComponentAmount, int unitId);

        IExtendedMaterialModel UpsertMaterial(int? materialId,
            string name,
            decimal nominalAmount,
            int nominalUnitId,
            int materialInventoryId,
            bool automaticBatches,
            bool requiresPrice,
            bool requiresProductionPrice,
            bool requiresInvoice,
            bool requiresSupplierReference, bool autofinalize);
        
        void CleanCache();
        
        void DeleteMaterial(int id);

        IEnumerable<IMaterialInventory> GetMaterialInventories();
        
        void EnsureCompatibleUnit(IExtendedMaterialModel material, IMaterialUnit unit);
    }

    public interface IMaterialRepositoryWithPostponedCache : IMaterialRepository, IDisposable { }
}
