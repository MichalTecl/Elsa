using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Inventory;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public class MaterialRepository : IMaterialRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly ICache m_cache;
        private readonly IUnitConversionHelper m_conversionHelper;

        private string MaterialsCacheKey => $"AllMaterialsBy_ProjectId={m_session.Project.Id}";
        private string VirtualProductCompositionsCacheKey => $"AllVPCompositionsBy_ProjectID={m_session.Project.Id}";

        public MaterialRepository(IDatabase database, ISession session, ICache cache, IUnitConversionHelper conversionHelper)
        {
            m_database = database;
            m_session = session;
            m_cache = cache;
            m_conversionHelper = conversionHelper;
        }

        public IExtendedMaterialModel GetMaterialById(int materialId)
        {
            var mat = GetAllMaterials().FirstOrDefault(m => m.Id == materialId);
            if (mat != null)
            {
                return mat;
            }

            m_cache.Remove(MaterialsCacheKey);
            m_cache.Remove(VirtualProductCompositionsCacheKey);

            mat = GetAllMaterials().FirstOrDefault(m => m.Id == materialId);
            if (mat != null)
            {
                return mat;
            }

            throw new InvalidOperationException("Invalid MaterialId");
        }

        public IExtendedMaterialModel GetMaterialByName(string materialName)
        {
            return GetAllMaterials().FirstOrDefault(m => m.Name == materialName);
        }

        public IEnumerable<MaterialComponent> GetMaterialsByVirtualProductId(int virtualProductId)
        {
            foreach (var composition in GetAllCompositions().Where(c => c.VirtualProductId == virtualProductId))
            {
                var material = GetMaterialById(composition.ComponentId);
                var batch = material.CreateBatch(composition.Amount, composition.Unit, m_conversionHelper);

                yield return new MaterialComponent(batch.BatchUnit, batch, batch.BatchAmount, composition.Id);
            }
        }

        public IEnumerable<IExtendedMaterialModel> GetAllMaterials()
        {
            return m_cache.ReadThrough(MaterialsCacheKey, TimeSpan.FromMinutes(1), GetAllMaterialsFromDatabase);
        }

        private IEnumerable<IExtendedMaterialModel> GetAllMaterialsFromDatabase()
        {
            var data = LoadMaterialEntities().Select(m => new ExtendedMaterial(m)).ToList();

            foreach (var extendedMaterial in data)
            {
                foreach (var composition in extendedMaterial.Adaptee.Composition)
                {
                    var child = data.FirstOrDefault(c => c.Id == composition.ComponentId);
                    if (child == null)
                    {
                        throw new InvalidOperationException("Invalid composition");
                    }

                    extendedMaterial.AddComponent(composition.Amount, composition.Unit, child);
                }
            }

            return data;
        }

        private IEnumerable<IVirtualProductMaterial> GetAllCompositions()
        {
            return m_cache.ReadThrough(
                VirtualProductCompositionsCacheKey,
                TimeSpan.FromMinutes(10),
                () =>
                    m_database.SelectFrom<IVirtualProductMaterial>()
                        .Join(v => v.Unit)
                        .Join(v => v.VirtualProduct)
                        .Where(vpm => vpm.VirtualProduct.ProjectId == m_session.Project.Id)
                        .Execute());
        }

        protected virtual IEnumerable<IMaterial> LoadMaterialEntities()
        {
            return m_database.SelectFrom<IMaterial>()
                    .Join(m => m.NominalUnit)
                    .Join(m => m.Composition)
                    .Join(m => m.Composition.Each().Unit)
                    .Join(m => m.VirtualProductMaterials)
                    .Where(m => m.ProjectId == m_session.Project.Id)
                    .Execute();
        }

        public void DetachMaterial(int virtualProductId, int materialId)
        {
            var prid = m_session.Project.Id;

            var srcMapping =
                m_database.SelectFrom<IVirtualProductMaterial>()
                    .Join(vpm => vpm.VirtualProduct)
                    .Join(vpm => vpm.Component)
                    .Where(vpm => vpm.VirtualProduct.ProjectId == prid && vpm.Component.ProjectId == prid)
                    .Where(vpm => vpm.VirtualProductId == virtualProductId)
                    .Where(vpm => vpm.ComponentId == materialId)
                    .Execute()
                    .FirstOrDefault();

            if (srcMapping == null)
            {
                return;
            }

            m_database.Delete(srcMapping);
            m_cache.Remove(VirtualProductCompositionsCacheKey);
        }

        public void AddOrUpdateComponent(int vpId, int materialId, decimal requestComponentAmount, int unitId)
        {
            
            var existing = GetAllCompositions().FirstOrDefault(c => c.VirtualProductId == vpId && c.ComponentId == materialId);
            if (existing == null)
            {
                existing = m_database.New<IVirtualProductMaterial>(v => v.VirtualProductId = vpId);
            }
            else
            {
                if (existing.ComponentId == materialId && existing.UnitId == unitId
                    && existing.Amount == requestComponentAmount)
                {
                    return;
                }
            }

            using (var tx = m_database.OpenTransaction())
            {
                existing.Amount = requestComponentAmount;
                existing.UnitId = unitId;
                existing.ComponentId = materialId;

                m_database.Save(existing);

                tx.Commit();
            }

            m_cache.Remove(VirtualProductCompositionsCacheKey);
        }

        public IMaterialRepositoryWithPostponedCache GetWithPostponedCache()
        {
            return new MaterialRepositoryWithPostponedCache(m_database, m_session, new CacheWithPostponedRemoval(m_cache), m_conversionHelper);
        }

        private sealed class MaterialRepositoryWithPostponedCache : MaterialRepository, IMaterialRepositoryWithPostponedCache
        {
            private readonly CacheWithPostponedRemoval m_ppCache;

            public MaterialRepositoryWithPostponedCache(IDatabase database, ISession session, CacheWithPostponedRemoval cache, IUnitConversionHelper conversionHelper)
                : base(database, session, cache, conversionHelper)
            {
                m_ppCache = cache;
            }

            public void Dispose()
            {
                m_ppCache.Dispose();
            }
        }
    }
}
