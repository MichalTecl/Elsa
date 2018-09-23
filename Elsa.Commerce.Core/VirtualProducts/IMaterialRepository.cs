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

        IEnumerable<IExtendedMaterialModel> GetAllMaterials();

        void DetachMaterial(int virtualProductId, int materialId);

        void AddOrUpdateComponent(int vpId, int materialId, decimal requestComponentAmount, int unitId);

        void CleanCache();
    }

    public interface IMaterialRepositoryWithPostponedCache : IMaterialRepository, IDisposable { }
}
