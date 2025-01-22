using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Inventory;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IMaterialRepository : ICanPostponeCacheRemovals<IMaterialRepositoryWithPostponedCache>
    {
        IExtendedMaterialModel GetMaterialById(int materialId);

        IExtendedMaterialModel GetMaterialByName(string materialName);

        IEnumerable<MaterialComponent> GetMaterialsByVirtualProductId(int virtualProductId);

        IEnumerable<IExtendedMaterialModel> GetAllMaterials(int? inventoryId, bool includeHidden);

        void DetachMaterial(int virtualProductId, int materialId);

        void AddOrUpdateComponent(int vpId, int materialId, decimal requestComponentAmount, int unitId);

        IExtendedMaterialModel UpsertMaterial(int? materialId, Action<IMaterial> setup);

        void CleanCache();
        
        void DeleteMaterial(int id);

        IEnumerable<IMaterialInventory> GetMaterialInventories();
        
        void EnsureCompatibleUnit(IExtendedMaterialModel material, IMaterialUnit unit);

        List<MaterialReportingGroupAssignmentModel> GetMaterialReportingGroupAssignments();

        void SaveMaterialReportingGroupAssignments(IEnumerable<MaterialReportingGroupAssignmentModel> models, out int groupsCreated, out int materialsAssigned);

        void SaveOrderDt(int materialId, DateTime? orderDt);

        void SaveMaterialComment(int materialId, string comment, UserRight writeCommentUserRight);

        IExtendedMaterialModel SetMaterialHidden(int id, bool hide, bool clearCache = true);
    }

    public interface IMaterialRepositoryWithPostponedCache : IMaterialRepository, IDisposable { }
}
