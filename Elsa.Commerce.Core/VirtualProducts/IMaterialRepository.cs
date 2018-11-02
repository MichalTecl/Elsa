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

        IExtendedMaterialModel UpsertMaterial(int? materialId, string name, decimal nominalAmount, int nominalUnitId);
        
        void CleanCache();

        void DetachMaterialComponent(int compositionMaterialId, int componentMaterialId);

        void SetMaterialComponent(
            int compositionMaterialId,
            int componentMaterialId,
            decimal componentAmount,
            int amountUnit);

        void DeleteMaterial(int id);

        IEnumerable<IMaterialInventory> GetMaterialInventories();
    }

    public interface IMaterialRepositoryWithPostponedCache : IMaterialRepository, IDisposable { }
}
