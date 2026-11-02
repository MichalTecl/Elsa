using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.CurrencyRates;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Production.Recipes;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse.Impl.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce.SaleEvents;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire;
using RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Impl
{
    public class MaterialBatchRepository : IMaterialBatchRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly IUnitConversionHelper _conversionHelper;
        private readonly IMaterialRepository _materialRepository;
        private readonly IVirtualProductRepository _virtualProductRepository;
        private readonly ICache _cache;
        private readonly Lazy<IMaterialBatchFacade> _materialBatchFacade;
        private readonly ISupplierRepository _supplierRepository;
        private readonly ICurrencyConversionHelper _currencyConversionHelper;
        private readonly IServiceLocator _serviceLocator;
        private readonly Lazy<IRecipeRepository> _recipeRepository;

        public MaterialBatchRepository(IDatabase database,
            ISession session,
            IUnitConversionHelper conversionHelper,
            IMaterialRepository materialRepository,
            IVirtualProductRepository virtualProductRepository,
            ICache cache,
            Lazy<IMaterialBatchFacade> materialBatchFacade,
            ISupplierRepository supplierRepository, 
            ICurrencyConversionHelper currencyConversionHelper, IServiceLocator serviceLocator, Lazy<IRecipeRepository> recipeRepository)
        {
            _database = database;
            _session = session;
            _conversionHelper = conversionHelper;
            _materialRepository = materialRepository;
            _virtualProductRepository = virtualProductRepository;
            _cache = cache;
            _materialBatchFacade = materialBatchFacade;
            _supplierRepository = supplierRepository;
            _currencyConversionHelper = currencyConversionHelper;
            _serviceLocator = serviceLocator;
            _recipeRepository = recipeRepository;
        }

        public MaterialBatchComponent GetBatchById(int id)
        {
            var entity = GetBatchQuery().Where(b => b.Id == id).Execute().FirstOrDefault();
            return entity == null ? null : MapToModel(entity);
        }

        public MaterialBatchComponent GetBatchByNumber(int materialId, string batchNumber)
        {
            var batch =
                GetBatchQuery()
                    .Where(b => (b.MaterialId == materialId) && (b.BatchNumber == batchNumber))
                    .Execute()
                    .FirstOrDefault();
            
            return MapToModel(batch);
        }

        private MaterialBatchComponent MapToModel(IMaterialBatch entity)
        {
            if (entity == null)
            {
                return null;
            }

            var model = new MaterialBatchComponent(new MaterialBatchAdapter(entity, _serviceLocator), this);
            return model;
        }

        public IEnumerable<MaterialBatchComponent> GetMaterialBatches(
            DateTime from,
            DateTime to,
            bool excludeCompositions,
            int? materialId,
            bool includeLocked = false,
            bool includeClosed = false,
            bool includeUnavailable = false)
        {
            var query = GetBatchQuery().Where(b => (b.Created >= from) && (b.Created <= to));
            
            if (materialId != null)
            {
                query = query.Where(b => b.MaterialId == materialId.Value);
            }

            if (!includeLocked)
            {
                query = query.Where(b => b.LockDt == null);
            }

            if (!includeClosed)
            {
                query = query.Where(b => b.CloseDt == null);
            }

            if (!includeUnavailable)
            {
                query = query.Where(b => b.IsAvailable);
            }

            var entities = query.Execute()
                .Where(b => (!excludeCompositions) || (!b.Components.Any()))
                .Select(MapToModel);

            return entities;
        }

        public IEnumerable<int> GetBatchIds(DateTime @from, DateTime to, int? materialId, bool includeLocked = false,
            bool includeClosed = false, bool includeUnavailable = false)
        {
            return QueryBatches(query =>
            {
                if (materialId != null)
                {
                    query = query.Where(b => b.MaterialId == materialId.Value);
                }

                if (!includeLocked)
                {
                    query = query.Where(b => b.LockDt == null);
                }

                if (!includeClosed)
                {
                    query = query.Where(b => b.CloseDt == null);
                }

                if (!includeUnavailable)
                {
                    query = query.Where(b => b.IsAvailable);
                }
            }).Select(b => b.Id);
        }

        public MaterialBatchComponent SaveBottomLevelMaterialBatch(int id, IMaterial material, decimal amount, IMaterialUnit unit, string batchNr, DateTime receiveDt, decimal price, string invoiceNr, string supplierName, string currencySymbol, string variableSymbol)
        {
            if ((material.ProjectId != _session.Project.Id) || (unit.ProjectId != _session.Project.Id))
            {
                throw new InvalidOperationException("Illegal entity reference");
            }
            
            int? supplierId = null;
            if (!string.IsNullOrWhiteSpace(supplierName))
            {
                var supplier =
                    _supplierRepository.GetSuppliers()
                        .FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.InvariantCultureIgnoreCase));

                if (supplier == null)
                {
                    throw new InvalidOperationException($"Nenalezen dodavatel \"{supplierName}\"");
                }

                supplierId = supplier.Id;
            }

            MaterialBatchComponent result;
            using (var tx = _database.OpenTransaction())
            {
                IMaterialBatch entity;
                if (id > 0)
                {
                    throw new InvalidOperationException("UPDATE not implemented => Currency conversion");

                    entity =
                        GetBatchQuery()
                            .Where(b => (b.Id == id) && (b.ProjectId == _session.Project.Id))
                            .Execute()
                            .FirstOrDefault();
                    if (entity == null)
                    {
                        throw new InvalidOperationException("Illegal entity reference");
                    }
                }
                else
                {
                    entity = _database.New<IMaterialBatch>();
                    entity.AuthorId = _session.User.Id;
                    entity.ProjectId = _session.Project.Id;
                    
                    var materialId = material.Id;

                    var existingBatchNr =
                        _database.SelectFrom<IMaterialBatch>()
                            .Where(
                                b =>
                                    (b.ProjectId == _session.Project.Id) && (b.MaterialId == materialId)
                                    && (b.BatchNumber == batchNr))
                            .Execute()
                            .FirstOrDefault();

                    if (existingBatchNr != null)
                    {
                        throw new InvalidOperationException("Již existuje šarže se zadaným číslem");
                    }

                }

                if (!_conversionHelper.AreCompatible(material.NominalUnitId, unit.Id))
                {
                    throw new InvalidOperationException(
                              $"Nelze použít jednotku '{unit.Symbol}' pro materiál '{material.Name}' protože není převoditelná na nominální jednotku materiálu '{material.NominalUnit}'");
                }

                if ((material.RequiresPrice == true) && (Math.Abs(price) < 0.000001m))
                {
                    throw new InvalidOperationException("Cena je povinný údaj");
                }

                if ((material.RequiresInvoiceNr == true) && string.IsNullOrWhiteSpace(invoiceNr))
                {
                    throw new InvalidOperationException("Číslo faktury je povinný údaj");
                }

                if ((material.RequiresInvoiceNr == true) && string.IsNullOrWhiteSpace(variableSymbol))
                {
                    throw new InvalidOperationException("Var. symbol je povinný údaj");
                }

                if ((material.RequiresSupplierReference == true) && (supplierId == null))
                {
                    throw new InvalidOperationException("Dodavatel je povinný údaj");
                }

                entity.BatchNumber = batchNr;
                entity.Created = receiveDt;
                entity.MaterialId = material.Id;
                entity.Volume = amount;
                entity.UnitId = unit.Id;
                
                entity.Price = _currencyConversionHelper.TryConvertToPrimaryCurrency(currencySymbol,
                    price,
                    c => entity.PriceConversionId = c.Id);
                
                entity.Note = string.Empty;
                entity.IsAvailable = true;
                entity.InvoiceNr = invoiceNr;
                entity.InvoiceVarSymbol = variableSymbol;
                entity.SupplierId = supplierId;
                
                _database.Save(entity);

                result = GetBatchById(entity.Id);

                if (result.Components.Any())
                {
                    throw new InvalidOperationException($"Materiál '{material.Name}' nelze naskladnit, protože se skládá z jiných materiálů. Použijte prosím funkci Výroba");
                }

                _materialBatchFacade.Value.ReleaseBatchAmountCache(result.Batch);

                tx.Commit();
            }

            return result;
        }

        public IEnumerable<IMaterialStockEvent> GetBatchEvents(int materialBatchId)
        {
            var key = $"stockEvents_for_batch_{materialBatchId}";

            return _cache.ReadThrough(
                key,
                TimeSpan.FromHours(1),
                () =>
                    _database.SelectFrom<IMaterialStockEvent>()
                        .Where(m => m.ProjectId == _session.Project.Id)
                        .Where(m => m.BatchId == materialBatchId)
                        .Execute());
        }

        public IEnumerable<IMaterialBatchComposition> GetCompositionsByComponentBatchId(int componentBatchId)
        {
            return
                _database.SelectFrom<IMaterialBatchComposition>()
                    .Join(c => c.Unit)
                    .Where(c => c.ComponentId == componentBatchId)
                    .Execute();
        }

        public void UpdateBatchAvailability(int batchId, bool isAvailable)
        {
            using (var tx = _database.OpenTransaction())
            {
                var batch =
                    _database.SelectFrom<IMaterialBatch>()
                        .Where(
                            b => (b.Id == batchId) && (b.IsAvailable != isAvailable) && (b.ProjectId == _session.Project.Id))
                        .Execute()
                        .FirstOrDefault();

                if (batch != null)
                {
                    batch.IsAvailable = isAvailable;
                    _database.Save(batch);
                }

                tx.Commit();
            }
        }

        public MaterialBatchComponent CreateProductionBatch(int materialId,
            string batchNumber,
            decimal amount,
            IMaterialUnit unit,
            decimal productionWorkPrice)
        {
            if (string.IsNullOrWhiteSpace(batchNumber))
            {
                throw new InvalidOperationException("Musí být číslo šarže");
            }

            if (unit == null)
            {
                throw new InvalidOperationException("Musí být vybrána měrná jednotka");
            }
            
            var material = _materialRepository.GetMaterialById(materialId);
            if (material == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            if (!_conversionHelper.AreCompatible(material.Adaptee.NominalUnitId, unit.Id))
            {
                throw new InvalidOperationException($"Požadovaná měrná jednotka \"{unit.Symbol}\" není platná pro materiál \"{material.Name}\" protože není převoditelná na nominální jednotku materiálu \"{material.NominalUnit.Symbol}\" ");
            }

            var alreadyExisting = GetMaterialBatches(
                DateTime.Now.AddYears(-100),
                DateTime.Now.AddYears(100),
                false,
                materialId,
                true,
                true,
                true).FirstOrDefault(m => m.Batch.BatchNumber.Equals(batchNumber, StringComparison.InvariantCultureIgnoreCase));
            if (alreadyExisting != null)
            {
                throw new InvalidOperationException($"Již existuje šarže {batchNumber} materiálu {material.Name}");
            }

            var entity = _database.New<IMaterialBatch>();
            entity.BatchNumber = batchNumber;
            entity.UnitId = unit.Id;
            entity.AuthorId = _session.User.Id;
            entity.Created = DateTime.Now;
            entity.ProjectId = _session.Project.Id;
            entity.MaterialId = material.Id;
            entity.Volume = amount;
            entity.Note = string.Empty;
            entity.ProductionWorkPrice = productionWorkPrice;

            _database.Save(entity);

            return GetBatchById(entity.Id);
        }

        public string GetBatchNumberById(int batchId)
        {
            return _cache.ReadThrough($"batchNrById_{_session.Project.Id}_{batchId}",
                TimeSpan.FromHours(24),
                () =>
                {
                    return
                        _database.SelectFrom<IMaterialBatch>()
                            .Where(b => (b.Id == batchId) && (b.ProjectId == _session.Project.Id))
                            .Take(1)
                            .Execute()
                            .FirstOrDefault()?.BatchNumber;
                });
        }

        public int? GetBatchIdByNumber(int materialId, string batchNumber)
        {
            return _cache.ReadThrough($"batchIdByNr_{materialId}_{batchNumber}",
                TimeSpan.FromHours(24),
                () =>
                    _database.SelectFrom<IMaterialBatch>()
                        .Where(b => b.ProjectId == _session.Project.Id)
                        .Where(b => b.BatchNumber == batchNumber)
                        .Where(b => b.MaterialId == materialId)
                        .Take(1)
                        .Execute()
                        .FirstOrDefault()?.Id);
        }

        public IEnumerable<IMaterialBatch> GetBatches(BatchKey key)
        {
            var materialId = key.GetMaterialId(this);
            var batchNumber = key.GetBatchNumber(this);

            var entities = _database.SelectFrom<IMaterialBatch>().Where(b => b.ProjectId == _session.Project.Id)
                .Where(b => b.CloseDt == null).Where(b => b.MaterialId == materialId && b.BatchNumber == batchNumber)
                .OrderBy(b => b.Created).Execute().ToList();

            return entities.Select(e => new MaterialBatchAdapter(e, _serviceLocator));
        }

        public IEnumerable<int> QueryBatchIds(Action<IQueryBuilder<IMaterialBatch>> customize)
        {
            var qry = _database.SelectFrom<IMaterialBatch>().Where(b => b.ProjectId == _session.Project.Id);

            customize(qry);

            return qry.Execute().Select(b => b.Id).Distinct();
        }

        public IEnumerable<IMaterialBatch> QueryBatches(Action<IQueryBuilder<IMaterialBatch>> customize)
        {
            var qry = _database.SelectFrom<IMaterialBatch>().Where(b => b.ProjectId == _session.Project.Id);

            customize(qry);

            return qry.Execute();
        }

        public MaterialBatchComponent UpdateBatch(int id, Action<IMaterialBatchEditables> edit)
        {
            var batch = GetBatchById(id).Ensure();

            edit(batch.Batch);

            _database.Save(batch.Batch);

            return GetBatchById(id);
        }
        
        public IEnumerable<MaterialBatchComponent> GetBatchesByComponentInventory(int componentMaterialInventoryId, int compositionYear, int compositionMonth)
        {
            var from = new DateTime(compositionYear, compositionMonth, 1).Date;
            var to = from.AddMonths(1);

            var compositionIds = _database.SelectFrom<IMaterialBatch>()
                .Join(b => b.Material)
                .Join(b => b.Components)
                .Join(b => b.Components.Each().Component)
                .Join(b => b.Components.Each().Component.Material)
                .Where(b => b.ProjectId == _session.Project.Id)
                .Where(b => b.Created >= from && b.Created < to)
                .Where(b => b.Components.Each().Component.Material.InventoryId == componentMaterialInventoryId)
                .Execute().Select(r => r.Id).Distinct();

            foreach (var compositionBatchId in compositionIds)
            {
                yield return GetBatchById(compositionBatchId);
            }
        }
        
        public IEnumerable<IMaterialBatchComposition> GetBatchComponents(int compositionId)
        {
            return _database.SelectFrom<IMaterialBatchComposition>().Where(c => c.CompositionId == compositionId)
                .Execute().Select(i => new MaterialBatchCompositionAdapter(_serviceLocator, i));
        }

        public IEnumerable<IMaterialBatch> GetBatchesByInvoiceNumber(string invoiceNumber, int supplierId)
        {
            return GetBatchQuery().Where(b => b.SupplierId == supplierId).Where(b => b.InvoiceNr == invoiceNumber)
                .Where(b => b.CloseDt == null).Execute()
                .Select(b => MapToModel(b).Batch);
        }

        public IMaterialBatch CreateBatchWithComponents(int recipeId, Amount amount, string batchNumber,
            decimal productionPrice,
            List<Tuple<BatchKey, Amount>> components, int? replaceBatchId)
        {
            var batchesToReleaseFromCache = new HashSet<int>();
            var newBatchId = -1;

            try
            {
                var compositionsToMove = new List<IMaterialBatchComposition>();
                var stockEventsToMove = new List<IMaterialStockEvent>();
                var saleEventsToMove = new List<ISaleEventAllocation>();
                var orderItemsToMove = new List<IOrderItemMaterialBatch>();
                var componentsToDelete = new List<IMaterialBatchComposition>();

                var createDt = DateTime.Now;
                var note = string.Empty;

                using (var tx = _database.OpenTransaction())
                {
                    if (replaceBatchId != null)
                    {
                        batchesToReleaseFromCache.Add(replaceBatchId.Value);

                        compositionsToMove.AddRange(_database.SelectFrom<IMaterialBatchComposition>().Where(c => c.ComponentId == replaceBatchId.Value).Execute());
                        stockEventsToMove.AddRange(_database.SelectFrom<IMaterialStockEvent>().Where(se => se.BatchId == replaceBatchId.Value).Execute());
                        saleEventsToMove.AddRange(_database.SelectFrom<ISaleEventAllocation>().Where(se => se.BatchId == replaceBatchId.Value).Execute());
                        orderItemsToMove.AddRange(_database.SelectFrom<IOrderItemMaterialBatch>().Where(i => i.MaterialBatchId == replaceBatchId.Value).Execute());

                        componentsToDelete.AddRange(_database.SelectFrom<IMaterialBatchComposition>().Where(c => c.CompositionId == replaceBatchId.Value).Execute());

                        batchesToReleaseFromCache.AddRange(compositionsToMove.Select(c => c.CompositionId));
                        batchesToReleaseFromCache.AddRange(componentsToDelete.Select(c => c.ComponentId));

                        var originalBatch = GetBatchById(replaceBatchId.Value).Ensure();

                        var canChangeBatchNumber = saleEventsToMove.Count == 0 && orderItemsToMove.Count == 0;

                        if ((!originalBatch.Batch.BatchNumber.Equals(batchNumber)) && (!canChangeBatchNumber))
                        {
                            throw new InvalidOperationException("Nelze změnit číslo šarže, protože již byl uskutečněn prodej");
                        }

                        createDt = originalBatch.Batch.Created;
                        note =
                            $"({originalBatch.Batch.Note ?? string.Empty}) ! {DateTime.Now} Nahrazení původního segmentu {originalBatch.Batch.Id}";
                    }
                    
                    var recipe = _recipeRepository.Value.GetRecipe(recipeId).Ensure("Recipe object expected");

                    var material = _materialRepository.GetMaterialById(recipe.ProducedMaterialId).Ensure("Produced material not found");
                    if (material.UniqueBatchNumbers == true)
                    {
                        var existingBatch = GetBatchByNumber(material.Id, batchNumber);
                        if (existingBatch?.Batch.Id != replaceBatchId)
                        {
                            throw new InvalidOperationException($"Šarže {batchNumber} již existuje a pro materiál {material.Name} je nastaven zákaz duplicitních čísel šarží");
                        }
                    }

                    var batch = _database.New<IMaterialBatch>();
                    batch.MaterialId = recipe.ProducedMaterialId;
                    batch.RecipeId = recipe.Id;
                    batch.AuthorId = _session.User.Id;
                    batch.IsAvailable = true;
                    batch.Produced = batch.Created = createDt;
                    batch.ProjectId = _session.Project.Id;
                    batch.ProductionWorkPrice = productionPrice;
                    batch.BatchNumber = batchNumber.Trim();
                    batch.Volume = amount.Value;
                    batch.Note = note;
                    batch.UnitId = amount.Unit.Id;

                    _database.Save(batch);

                    newBatchId = batch.Id;
                    batchesToReleaseFromCache.Add(batch.Id);

                    foreach (var component in components)
                    {
                        var assignments = _materialBatchFacade.Value.ResolveMaterialDemand(
                            component.Item1.GetMaterialId(this),
                            component.Item2,
                            component.Item1.GetBatchNumber(this),
                            false,
                            false,
                            createDt,
                            replaceBatchId);

                        if (!assignments.CompletelyAllocated)
                        {
                            throw new InvalidOperationException(
                                $"Není dostupné požadované množství v šarži {component.Item1.GetBatchNumber(this)}");
                        }

                        if (assignments.Allocations.Count != 1)
                        {
                            throw new InvalidOperationException("Neocekavana chyba");
                        }

                        foreach (var componentBatch in assignments.Allocations.Single().BatchIdAllocations)
                        {
                            var composition = _database.New<IMaterialBatchComposition>();
                            composition.ComponentId = componentBatch.Item1;
                            composition.CompositionId = batch.Id;
                            composition.Volume = componentBatch.Item2.Value;
                            composition.UnitId = componentBatch.Item2.Unit.Id;

                            _database.Save(composition);
                            batchesToReleaseFromCache.Add(composition.ComponentId);
                        }
                    }

                    _database.DeleteAll(componentsToDelete);

                    foreach (var ctm in compositionsToMove)
                    {
                        ctm.ComponentId = newBatchId;
                        _database.Save(ctm);
                    }

                    foreach (var stm in stockEventsToMove)
                    {
                        stm.BatchId = newBatchId;
                        _database.Save(stm);
                    }

                    foreach (var sae in saleEventsToMove)
                    {
                        sae.BatchId = newBatchId;
                        _database.Save(sae);
                    }

                    foreach (var otm in orderItemsToMove)
                    {
                        otm.MaterialBatchId = newBatchId;
                        _database.Save(otm);
                    }

                    if (replaceBatchId != null)
                    {
                        var btd = _database.SelectFrom<IMaterialBatch>().Where(b => b.Id == replaceBatchId.Value)
                            .Execute().Single();

                        _database.Delete(btd);
                    }

                    tx.Commit();
                }
            }
            finally
            {
                foreach (var bid in batchesToReleaseFromCache)
                {
                    _materialBatchFacade.Value.ReleaseBatchAmountCache(bid);
                }
            }

            return GetBatchById(newBatchId)?.Batch;
        }

        private IQueryBuilder<IMaterialBatch> GetBatchQuery()
        {
            return
                _database.SelectFrom<IMaterialBatch>()
                    .Where(b => b.ProjectId == _session.Project.Id);
        }

        public Tuple<int, string> GetBatchNumberAndMaterialIdByBatchId(int batchId)
        {
            return _cache.ReadThrough($"batchkey_{batchId}", TimeSpan.FromHours(1), () =>
            {
                var batch = GetBatchById(batchId).Ensure();

                return new Tuple<int, string>(batch.Batch.MaterialId, batch.Batch.BatchNumber);
            });
        }
    }
}
