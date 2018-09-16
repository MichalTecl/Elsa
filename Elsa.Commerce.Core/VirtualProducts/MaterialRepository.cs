using System;
using System.Collections.Generic;
using System.Linq;

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

        private string MaterialsCacheKey => $"AllMaterialsBy_ProjectId={m_session.Project.Id}";
        private string VirtualProductCompositionsCacheKey => $"AllVPCompositionsBy_ProjectID={m_session.Project.Id}";

        public MaterialRepository(IDatabase database, ISession session, ICache cache)
        {
            m_database = database;
            m_session = session;
            m_cache = cache;
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

        public IEnumerable<MaterialComponent> GetMaterialsByVirtualProductId(int virtualProductId)
        {
            foreach (var composition in GetAllCompositions().Where(c => c.VirtualProductId == virtualProductId))
            {
                yield return new MaterialComponent(composition.Unit, GetMaterialById(composition.ComponentId), composition.Amount);
            }
        }

        private IEnumerable<IExtendedMaterialModel> GetAllMaterials()
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
    }
}
