using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.EntityComments;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Recipes;
using RobOrm.Core;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public class MaterialRepository : IMaterialRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ICache _cache;
        private readonly IUnitConversionHelper _conversionHelper;
        private readonly IEntityCommentsFacade _entityComments;
        
        private string MaterialsCacheKey => $"AllMaterialsBy_ProjectId={_session.Project.Id}";
        private string VirtualProductCompositionsCacheKey => $"AllVPCompositionsBy_ProjectID={_session.Project.Id}";

        public MaterialRepository(IDatabase database, ISession session, ICache cache, IUnitConversionHelper conversionHelper, IEntityCommentsFacade entityComments)
        {
            _database = database;
            _session = session;
            _cache = cache;
            _conversionHelper = conversionHelper;
            _entityComments = entityComments;
        }

        public IExtendedMaterialModel GetMaterialById(int materialId)
        {
            var mat = GetAllMaterials(null, true).FirstOrDefault(m => m.Id == materialId);
            if (mat != null)
            {
                return mat;
            }

            _cache.Remove(MaterialsCacheKey);
            _cache.Remove(VirtualProductCompositionsCacheKey);

            mat = GetAllMaterials(null, true).FirstOrDefault(m => m.Id == materialId);
            if (mat != null)
            {
                return mat;
            }

            throw new InvalidOperationException("Invalid MaterialId");
        }

        public IExtendedMaterialModel GetMaterialByName(string materialName)
        {
            return GetAllMaterials(null, true).FirstOrDefault(m => m.Name == materialName);
        }

        public IEnumerable<MaterialComponent> GetMaterialsByVirtualProductId(int virtualProductId)
        {
            foreach (var composition in GetAllCompositions().Where(c => c.VirtualProductId == virtualProductId))
            {
                var material = GetMaterialById(composition.ComponentId);
                var batch = material.CreateBatch(composition.Amount, composition.Unit, _conversionHelper);

                yield return new MaterialComponent(batch.BatchUnit, batch, batch.BatchAmount, composition.Id);
            }
        }

        public IEnumerable<IExtendedMaterialModel> GetAllMaterials(int? inventoryId, bool includeHidden)
        {
            var all = _cache.ReadThrough(MaterialsCacheKey, TimeSpan.FromMinutes(10), GetAllMaterialsFromDatabase);

            if (inventoryId != null)
            {
                all = all.Where(i => i.InventoryId == inventoryId.Value);                
            }

            if (!includeHidden)
            {
                all = all.Where(m => !m.IsHidden);
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
            return _cache.ReadThrough(
                VirtualProductCompositionsCacheKey,
                TimeSpan.FromMinutes(10),
                () =>
                    _database.SelectFrom<IVirtualProductMaterial>()
                        .Join(v => v.Unit)
                        .Join(v => v.VirtualProduct)
                        .Where(vpm => vpm.VirtualProduct.ProjectId == _session.Project.Id)
                        .Execute());
        }

        protected virtual IEnumerable<IMaterial> LoadMaterialEntities()
        {
            return _database.SelectFrom<IMaterial>()
                    .Join(m => m.NominalUnit)
                    .Join(m => m.VirtualProductMaterials)
                    .Join(m => m.Thresholds)
                    .Join(m => m.Thresholds.Each().Unit)
                    .Join(m => m.Inventory)
                    .Where(m => m.ProjectId == _session.Project.Id)
                    .Execute();
        }

        public void DetachMaterial(int virtualProductId, int materialId)
        {
            var prid = _session.Project.Id;

            var srcMapping =
                _database.SelectFrom<IVirtualProductMaterial>()
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

            _database.Delete(srcMapping);
            _cache.Remove(VirtualProductCompositionsCacheKey);
        }

        public void AddOrUpdateComponent(int vpId, int materialId, decimal requestComponentAmount, int unitId)
        {
            
            var existing = GetAllCompositions().FirstOrDefault(c => (c.VirtualProductId == vpId) && (c.ComponentId == materialId));
            if (existing == null)
            {
                existing = _database.New<IVirtualProductMaterial>(v => v.VirtualProductId = vpId);
            }
            else
            {
                if ((existing.ComponentId == materialId) && (existing.UnitId == unitId)
                    && (existing.Amount == requestComponentAmount))
                {
                    return;
                }
            }

            using (var tx = _database.OpenTransaction())
            {
                existing.Amount = requestComponentAmount;
                existing.UnitId = unitId;
                existing.ComponentId = materialId;

                _database.Save(existing);

                tx.Commit();
            }

            _cache.Remove(VirtualProductCompositionsCacheKey);
        }

        public IExtendedMaterialModel UpsertMaterial(int? materialId, Action<IMaterial> setup)
        {
            IMaterial material;
            if (materialId != null)
            {
                material = GetAllMaterials(null, true).FirstOrDefault(m => m.Id == materialId.Value)?.Adaptee;
                if (material == null)
                {
                    throw new InvalidOperationException($"Invalid material Id {materialId}");
                }
                
                var oldNominalUnitId = material.NominalUnitId;

                setup(material);

                if (material.NominalUnitId != oldNominalUnitId)
                {
                    if (
                        _database.SelectFrom<IRecipe>()
                            .Where(mc => mc.ProducedMaterialId == materialId)
                            .Execute()
                            .Any())
                    {
                        throw new InvalidOperationException($"Není možné změnit nominální jednotku materiálu, protože tento materiál je již součástí receptury");
                    }

                    if (
                        _database.SelectFrom<IVirtualProductMaterial>()
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
                material = _database.New<IMaterial>();

                setup(material);

                if (GetAllMaterials(null, true).Any(m => m.Name.Equals(material.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new InvalidOperationException("Název materiálu byl již použit");
                }
            }

            var inventory = GetMaterialInventories().FirstOrDefault(i => i.Id == material.InventoryId);
            if (inventory == null)
            {
                throw new InvalidOperationException("Invalid reference to inventory");
            }
                        
            material.ProjectId = _session.Project.Id;            
            material.UnusedWarnMaterialType = string.IsNullOrWhiteSpace(material.UnusedWarnMaterialType) ? null : material.UnusedWarnMaterialType;
            
            _database.Save(material);

            CleanCache();

            return GetAllMaterials(null, true).Single(m => m.Id == material.Id);
        }

        public IExtendedMaterialModel UpsertMaterial(int? materialId, string name, decimal nominalAmount,
            int nominalUnitId, int materialInventoryId, bool automaticBatches, bool requiresPrice,
            bool requiresProductionPrice, bool requiresInvoice, bool requiresSupplierReference, bool autofinalize, bool canBeDigital,
            int? daysBeforeWarnForUnused, string unusedWarnMaterialType, bool usageProlongsLifetime)
        {
            IMaterial material;
            if (materialId != null)
            {
                material = GetAllMaterials(null, true).FirstOrDefault(m => m.Id == materialId.Value)?.Adaptee;
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
                    && (material.CanBeDigitalOnly ?? false == canBeDigital)
                    && (material.DaysBeforeWarnForUnused == daysBeforeWarnForUnused)
                    && (material.UnusedWarnMaterialType == unusedWarnMaterialType)
                    && (material.UsageProlongsLifetime ?? false == usageProlongsLifetime))
                {
                    return GetAllMaterials(null, true).Single(m => m.Id == materialId);
                }

                if (material.NominalUnitId != nominalUnitId)
                {
                    if (
                        _database.SelectFrom<IRecipe>()
                            .Where(mc => mc.ProducedMaterialId == materialId)
                            .Execute()
                            .Any())
                    {
                        throw new InvalidOperationException($"Není možné změnit nominální jednotku materiálu, protože tento materiál je již součástí receptury");
                    }

                    if (
                        _database.SelectFrom<IVirtualProductMaterial>()
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
                if (GetAllMaterials(null, true).Any(m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new InvalidOperationException("Název materiálu byl již použit");
                }
                
                material = _database.New<IMaterial>();
            }

            var inventory = GetMaterialInventories().FirstOrDefault(i => i.Id == materialInventoryId);
            if (inventory == null)
            {
                throw new InvalidOperationException("Invalid reference to inventory");
            }

            material.Name = name;
            material.NominalUnitId = nominalUnitId;
            material.NominalAmount = nominalAmount;
            material.ProjectId = _session.Project.Id;
            material.InventoryId = inventory.Id;
            material.AutomaticBatches = automaticBatches;
            material.RequiresPrice = requiresPrice;
            material.RequiresProductionPrice = requiresProductionPrice;
            material.RequiresInvoiceNr = requiresInvoice;
            material.RequiresSupplierReference = requiresSupplierReference;
            material.UseAutofinalization = autofinalize;
            material.CanBeDigitalOnly = canBeDigital;
            material.UnusedWarnMaterialType = string.IsNullOrWhiteSpace(unusedWarnMaterialType) ? null : unusedWarnMaterialType;
            material.DaysBeforeWarnForUnused = daysBeforeWarnForUnused;
            material.UsageProlongsLifetime = usageProlongsLifetime;

            _database.Save(material);

            CleanCache();

            return GetAllMaterials(null, true).Single(m => m.Id == material.Id);
        }

        public void CleanCache()
        {
            _cache.Remove(VirtualProductCompositionsCacheKey);
            _cache.Remove(MaterialsCacheKey);
        }
        
        public void DeleteMaterial(int id)
        {
            using (var tx = _database.OpenTransaction())
            {
                var material =
                    _database.SelectFrom<IMaterial>()
                        .Where(m => m.ProjectId == _session.Project.Id)
                        .Where(m => m.Id == id)
                        .Execute()
                        .FirstOrDefault();

                if (material == null)
                {
                    throw new InvalidOperationException("Material not found");
                }

                var toVp = _database.SelectFrom<IVirtualProductMaterial>().Where(t => t.ComponentId == id).Execute();
                var conns =
                    _database.SelectFrom<IRecipe>()
                        .Where(c => c.ProducedMaterialId == id)
                        .Execute();

                if (conns.Any())
                {
                    throw new InvalidOperationException("Nelze smazat materiál, který má recepturu");
                }

                var thresholds = _database.SelectFrom<IMaterialThreshold>().Where(t => t.ProjectId == _session.Project.Id && t.MaterialId == id).Execute();

                _database.DeleteAll(thresholds);
                _database.DeleteAll(toVp);
                _database.Delete(material);

                tx.Commit();

                CleanCache();
            }
        }

        public IEnumerable<IMaterialInventory> GetMaterialInventories()
        {
            return _cache.ReadThrough(
                $"allMatInventories_{_session.Project.Id}",
                TimeSpan.FromHours(1),
                () =>
                    _database.SelectFrom<IMaterialInventory>()
                        .Join(i => i.AllowedUnit)
                        .Where(i => i.ProjectId == _session.Project.Id)
                        .Execute());
        }
        
        public void EnsureCompatibleUnit(IExtendedMaterialModel material, IMaterialUnit unit)
        {
            if (!_conversionHelper.AreCompatible(material.NominalUnit.Id, unit.Id))
            {
                throw new InvalidOperationException($"Jednotku '{unit.Symbol}' nelze použít pro materiál '{material.Name}'");
            }
        }

        public IMaterialRepositoryWithPostponedCache GetWithPostponedCache()
        {
            return new MaterialRepositoryWithPostponedCache(_database, _session, new CacheWithPostponedRemoval(_cache), _conversionHelper, _entityComments);
        }

        public List<MaterialReportingGroupAssignmentModel> GetMaterialReportingGroupAssignments()
        {
            return _database.Sql().ExecuteWithParams(@"SELECT m.Name, rmg.Name
                                                FROM Material m
                                                LEFT JOIN ReportingMaterialGroupMaterial rmgm ON (m.Id = rmgm.MaterialId)
                                                LEFT JOIN ReportingMaterialGroup rmg ON (rmgm.GroupId = rmg.Id)
                                                JOIN MaterialInventory mi ON (m.InventoryId = mi.Id)
                                                WHERE mi.CanBeConnectedToTag = 1
                                                AND m.ProjectId = {0}
                                                ORDER BY m.Name", _session.Project.Id)
                .MapRows<MaterialReportingGroupAssignmentModel>(rdr => new MaterialReportingGroupAssignmentModel
                {
                    MaterialName = rdr.GetString(0),
                    ReportingGroupName = rdr.IsDBNull(1) ? null : rdr.GetString(1)
                })
                .ToList();            
        }

        public void SaveMaterialReportingGroupAssignments(IEnumerable<MaterialReportingGroupAssignmentModel> models, out int groupsCreated, out int materialsAssigned)
        {
            groupsCreated = 0;
            materialsAssigned = 0;

            var materialGroups = _database.SelectFrom<IReportingMaterialGroup>()
                .Where(g => g.ProjectId == _session.Project.Id)
                .Execute()
                .ToList();

            var materials = _database.SelectFrom<IMaterial>().Where(m => m.ProjectId == _session.Project.Id).Execute().ToList();
            var assignments = _database.SelectFrom<IReportingMaterialGroupMaterial>().Execute().ToList();

            using(var tx = _database.OpenTransaction())
            {
                foreach(var model in models)
                {
                    var material = materials.FirstOrDefault(m => m.Name ==  model.MaterialName) ?? throw new Exception($"Neznámý materiál '{model.MaterialName}'");
                    var group = materialGroups.FirstOrDefault(g => g.Name.Equals(model.ReportingGroupName, StringComparison.InvariantCultureIgnoreCase));

                    if (group == null && !string.IsNullOrWhiteSpace(model.ReportingGroupName))
                    {
                        groupsCreated++;
                        group = _database.New<IReportingMaterialGroup>();
                        group.ProjectId = _session.Project.Id;
                        group.Name = model.ReportingGroupName;
                        group.DisplayOrder = materialGroups.Max(g => g.DisplayOrder ?? 0) + 1;

                        _database.Save(group);
                        materialGroups.Add(group);
                    }

                    var existingAssignment = assignments.FirstOrDefault(a => a.MaterialId == material.Id);

                    if (existingAssignment?.GroupId == group?.Id)
                        continue;

                    materialsAssigned++;

                    if (existingAssignment != null)
                    {
                        _database.Delete(existingAssignment);
                        assignments.Remove(existingAssignment);
                    }

                    if (group != null)
                    {
                        var assignment = _database.New<IReportingMaterialGroupMaterial>();
                        assignment.MaterialId = material.Id;
                        assignment.GroupId = group.Id;

                        _database.Save(assignment);
                        assignments.Add(assignment);
                    }
                }

                tx.Commit();
            }

        }

        public void SaveOrderDt(int materialId, DateTime? orderDt)
        {
            //(@materialId INT, @orderDt DATETIME, @userId INT)
            _database.Sql()
                .Call("SaveMaterialOrderDt")
                .WithParam("@materialId", materialId)
                .WithParam("@orderDt", orderDt)
                .WithParam("@userId", _session.User.Id)
                .NonQuery();
        }

        public void SaveMaterialComment(int materialId, string comment, UserRight writeCommentUserRight)
        {
            var mat = GetMaterialById(materialId).Ensure();

            if (mat.CommentText == comment)
                return;

            mat.CommentText = comment;

            _entityComments.AddComment(writeCommentUserRight, mat);

            CleanCache();
        }

        public IExtendedMaterialModel SetMaterialHidden(int id, bool hide, bool clearCache = true)
        {
            var mat = GetMaterialById(id);
            if (mat.IsHidden == hide)
                return mat;

            var dbRecord = mat.Adaptee;

            dbRecord.HideUserId = hide ? (int?)_session.User.Id : null;
            dbRecord.HideDt = hide ? (DateTime?)DateTime.Now : null;

            _database.Save(dbRecord);

            // there is so much stuff related to material visibility...
            if (clearCache)
            {
                _cache.Clear();
            }
            else
            {
                // but sometimes...
                CleanCache();
            }

            return GetMaterialById(id);
        }

        private sealed class MaterialRepositoryWithPostponedCache : MaterialRepository, IMaterialRepositoryWithPostponedCache
        {
            private readonly CacheWithPostponedRemoval _ppCache;

            public MaterialRepositoryWithPostponedCache(IDatabase database, ISession session, CacheWithPostponedRemoval cache, IUnitConversionHelper conversionHelper, IEntityCommentsFacade comments)
                : base(database, session, cache, conversionHelper, comments)
            {
                _ppCache = cache;
            }

            public void Dispose()
            {
                _ppCache.Dispose();
            }
        }
    }
}
