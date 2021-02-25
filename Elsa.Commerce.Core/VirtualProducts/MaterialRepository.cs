using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Recipes;
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
            var mat = GetAllMaterials(null).FirstOrDefault(m => m.Id == materialId);
            if (mat != null)
            {
                return mat;
            }

            m_cache.Remove(MaterialsCacheKey);
            m_cache.Remove(VirtualProductCompositionsCacheKey);

            mat = GetAllMaterials(null).FirstOrDefault(m => m.Id == materialId);
            if (mat != null)
            {
                return mat;
            }

            throw new InvalidOperationException("Invalid MaterialId");
        }

        public IExtendedMaterialModel GetMaterialByName(string materialName)
        {
            return GetAllMaterials(null).FirstOrDefault(m => m.Name == materialName);
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

        public IEnumerable<IExtendedMaterialModel> GetAllMaterials(int? inventoryId)
        {
            var all = m_cache.ReadThrough(MaterialsCacheKey, TimeSpan.FromMinutes(10), GetAllMaterialsFromDatabase);

            if (inventoryId != null)
            {
                all = all.Where(i => i.InventoryId == inventoryId.Value);
            }

            return all;
        }

        private IEnumerable<IExtendedMaterialModel> GetAllMaterialsFromDatabase()
        {
            var data = LoadMaterialEntities().Select(m => new ExtendedMaterial(m)).ToList();
            
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
                    .Join(m => m.VirtualProductMaterials)
                    .Join(m => m.Thresholds)
                    .Join(m => m.Thresholds.Each().Unit)
                    .Join(m => m.Inventory)
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
                    .Where(vpm => (vpm.VirtualProduct.ProjectId == prid) && (vpm.Component.ProjectId == prid))
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
            
            var existing = GetAllCompositions().FirstOrDefault(c => (c.VirtualProductId == vpId) && (c.ComponentId == materialId));
            if (existing == null)
            {
                existing = m_database.New<IVirtualProductMaterial>(v => v.VirtualProductId = vpId);
            }
            else
            {
                if ((existing.ComponentId == materialId) && (existing.UnitId == unitId)
                    && (existing.Amount == requestComponentAmount))
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

        public IExtendedMaterialModel UpsertMaterial(int? materialId, string name, decimal nominalAmount,
            int nominalUnitId, int materialInventoryId, bool automaticBatches, bool requiresPrice,
            bool requiresProductionPrice, bool requiresInvoice, bool requiresSupplierReference, bool autofinalize, bool canBeDigital)
        {
            IMaterial material;
            if (materialId != null)
            {
                material = GetAllMaterials(null).FirstOrDefault(m => m.Id == materialId.Value)?.Adaptee;
                if (material == null)
                {
                    throw new InvalidOperationException($"Invalid material Id {materialId}");
                }

                if ((material.Name == name) 
                    && (material.NominalAmount == nominalAmount)
                    && (material.NominalUnitId == nominalUnitId) 
                    && (material.InventoryId == materialInventoryId)
                    && (material.AutomaticBatches == automaticBatches)
                    && (material.RequiresPrice == requiresPrice)
                    && (material.RequiresProductionPrice == requiresProductionPrice)
                    && (material.RequiresInvoiceNr == requiresInvoice)
                    && (material.RequiresSupplierReference == requiresSupplierReference)
                    && (material.UseAutofinalization ?? false == autofinalize)
                    && (material.CanBeDigitalOnly ?? false == canBeDigital))
                {
                    return GetAllMaterials(null).Single(m => m.Id == materialId);
                }

                if (material.NominalUnitId != nominalUnitId)
                {
                    if (
                        m_database.SelectFrom<IRecipe>()
                            .Where(mc => mc.ProducedMaterialId == materialId)
                            .Execute()
                            .Any())
                    {
                        throw new InvalidOperationException($"Není možné změnit nominální jednotku materiálu, protože tento materiál je již součástí receptury");
                    }

                    if (
                        m_database.SelectFrom<IVirtualProductMaterial>()
                            .Where(mc => mc.ComponentId == materialId)
                            .Execute()
                            .Any())
                    {
                        throw new InvalidOperationException($"Není možné změnit nominální jednotku materiálu, protože tento materiál je přiřazen k některému tagu");
                    }
                }

            }
            else
            {
                if (GetAllMaterials(null).Any(m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new InvalidOperationException("Název materiálu byl již použit");
                }
                
                material = m_database.New<IMaterial>();
            }

            var inventory = GetMaterialInventories().FirstOrDefault(i => i.Id == materialInventoryId);
            if (inventory == null)
            {
                throw new InvalidOperationException("Invalid reference to inventory");
            }

            material.Name = name;
            material.NominalUnitId = nominalUnitId;
            material.NominalAmount = nominalAmount;
            material.ProjectId = m_session.Project.Id;
            material.InventoryId = inventory.Id;
            material.AutomaticBatches = automaticBatches;
            material.RequiresPrice = requiresPrice;
            material.RequiresProductionPrice = requiresProductionPrice;
            material.RequiresInvoiceNr = requiresInvoice;
            material.RequiresSupplierReference = requiresSupplierReference;
            material.UseAutofinalization = autofinalize;
            material.CanBeDigitalOnly = canBeDigital;

            m_database.Save(material);

            CleanCache();

            return GetAllMaterials(null).Single(m => m.Id == material.Id);
        }

        public void CleanCache()
        {
            m_cache.Remove(VirtualProductCompositionsCacheKey);
            m_cache.Remove(MaterialsCacheKey);
        }
        
        public void DeleteMaterial(int id)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var material =
                    m_database.SelectFrom<IMaterial>()
                        .Where(m => m.ProjectId == m_session.Project.Id)
                        .Where(m => m.Id == id)
                        .Execute()
                        .FirstOrDefault();

                if (material == null)
                {
                    throw new InvalidOperationException("Material not found");
                }

                var toVp = m_database.SelectFrom<IVirtualProductMaterial>().Where(t => t.ComponentId == id).Execute();
                var conns =
                    m_database.SelectFrom<IRecipe>()
                        .Where(c => c.ProducedMaterialId == id)
                        .Execute();

                if (conns.Any())
                {
                    throw new InvalidOperationException("Nelze smazat materiál, který má recepturu");
                }

                m_database.DeleteAll(toVp);
                m_database.Delete(material);

                tx.Commit();

                CleanCache();
            }
        }

        public IEnumerable<IMaterialInventory> GetMaterialInventories()
        {
            return m_cache.ReadThrough(
                $"allMatInventories_{m_session.Project.Id}",
                TimeSpan.FromHours(1),
                () =>
                    m_database.SelectFrom<IMaterialInventory>()
                        .Join(i => i.AllowedUnit)
                        .Where(i => i.ProjectId == m_session.Project.Id)
                        .Execute());
        }
        
        public void EnsureCompatibleUnit(IExtendedMaterialModel material, IMaterialUnit unit)
        {
            if (!m_conversionHelper.AreCompatible(material.NominalUnit.Id, unit.Id))
            {
                throw new InvalidOperationException($"Jednotku '{unit.Symbol}' nelze použít pro materiál '{material.Name}'");
            }
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
