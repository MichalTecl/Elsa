using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Media.Animation;
using Elsa.Commerce.Core.Crm;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Model.BatchPriceExpl;
using Elsa.Commerce.Core.Production.Recipes;
using Elsa.Commerce.Core.Repositories;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Commerce.Core.Warehouse.Impl.Model;
using Elsa.Commerce.Core.Warehouse.Thresholds;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Impl
{
    public class MaterialBatchFacade : IMaterialBatchFacade, IBatchPriceBulkProvider
    {
        private readonly ILog _log;
        private readonly IVirtualProductFacade _virtualProductFacade;
        private readonly IMaterialBatchRepository _batchRepository;
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly AmountProcessor _amountProcessor;
        private readonly ICache _cache;
        private readonly IDatabase _database;
        private readonly IPackingPreferredBatchRepository _batchPreferrenceRepository;
        private readonly IKitProductRepository _kitProductRepository;
        private readonly IUnitConversionHelper _conversionHelper;
        private readonly IMaterialThresholdRepository _materialThresholdRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IStockEventRepository _stockEventRepository;
        private readonly ISession _session;
        private readonly IFixedCostRepository _fixedCostRepository;
        private readonly IRecipeRepository _recipeRepository;
        private readonly ICustomerRepository _customerRepository;

        public MaterialBatchFacade(
            ILog log,
            IVirtualProductFacade virtualProductFacade,
            IMaterialBatchRepository batchRepository,
            IPurchaseOrderRepository orderRepository,
            AmountProcessor amountProcessor,
            ICache cache,
            IDatabase database,
            IPackingPreferredBatchRepository batchPreferrenceRepository,
            IKitProductRepository kitProductRepository,
            IUnitConversionHelper conversionHelper,
            IMaterialThresholdRepository materialThresholdRepository,
            IMaterialRepository materialRepository,
            IUnitRepository unitRepository,
            IStockEventRepository stockEventRepository,
            ISession session,
            IFixedCostRepository fixedCostRepository,
            IRecipeRepository recipeRepository,
            ICustomerRepository customerRepository)
        {
            _log = log;
            _virtualProductFacade = virtualProductFacade;
            _batchRepository = batchRepository;
            _orderRepository = orderRepository;
            _amountProcessor = amountProcessor;
            _cache = cache;
            _database = database;
            _batchPreferrenceRepository = batchPreferrenceRepository;
            _kitProductRepository = kitProductRepository;
            _conversionHelper = conversionHelper;
            _materialThresholdRepository = materialThresholdRepository;
            _materialRepository = materialRepository;
            _unitRepository = unitRepository;
            _stockEventRepository = stockEventRepository;
            _session = session;
            _fixedCostRepository = fixedCostRepository;
            _recipeRepository = recipeRepository;
            _customerRepository = customerRepository;
        }

        public void AssignOrderItemToBatch(int batchId, IPurchaseOrder order, long orderItemId, decimal assignmentQuantity, out string batchChangeWarnMessage)
        {
            batchChangeWarnMessage = null;
                        
            var orderItem = GetAllOrderItems(order).FirstOrDefault(i => i.Id == orderItemId);
            if (orderItem == null)
            {
                throw new InvalidOperationException("Invalid OrderItemId");
            }

            var materialAmount = _virtualProductFacade.GetOrderItemMaterialForSingleUnit(order, orderItem);
            
            var batch = _batchRepository.GetBatchById(batchId);
            if (batch == null)
            {
                throw new InvalidOperationException("Invalid batch reference");
            }

            if (materialAmount.MaterialId != batch.Batch.MaterialId)
            {
                throw new InvalidOperationException("Batch material mismatch");
            }

            if (batch.IsLocked)
            {
                throw new InvalidOperationException($"Šarže '{batch.Batch?.BatchNumber}' je zamčená");
            }

            if (batch.IsClosed)
            {
                throw new InvalidOperationException($"Šarže '{batch.Batch?.BatchNumber}' je uzavřená");
            }
                        
            ReleaseBatchAmountCache(batchId);

            using (var tx = _database.OpenTransaction())
            {
                var available = GetAvailableAmount(batchId);

                var subtracted = _amountProcessor.Subtract(
                    available,
                    new Amount(assignmentQuantity, materialAmount.Amount.Unit));

                if (subtracted.Value < 0m)
                {
                    throw new InvalidOperationException($"Požadované množství {new Amount(assignmentQuantity, materialAmount.Amount.Unit)} již není k dispozici v šarži {batch.Batch.BatchNumber}");
                }

                batchChangeWarnMessage = CheckChange(batchId, materialAmount);

                _orderRepository.UpdateOrderItemBatch(orderItem, batchId, assignmentQuantity);

                tx.Commit();

                ReleaseBatchAmountCache(batchId);
            }
        }

        private string CheckChange(int batchId, MaterialAmountModel material)
        {
            try
            {
                if (CheckBatchChange(material.MaterialId, batchId, out var oldBatchInfo))
                {
                    _log.Info($"Possible batch change detected");
                    var newBatchNr = _batchRepository.GetBatchNumberById(batchId);
                    if (newBatchNr == oldBatchInfo?.Item1)
                    {
                        _log.Info($"But the batch number is the same, so no change is needed (oldBatchId={oldBatchInfo?.Item2}, newBatchId={batchId})");
                        return null;
                    }
                    else if (oldBatchInfo == null)
                    {
                        _log.Info($"But there was no batch assigned yet, so no change warn needed");
                        return null;
                    }
                    else
                    {
                        var oldBatchAmount = GetAvailableAmount(oldBatchInfo.Item2);
                        if (oldBatchAmount.IsZero)
                        {
                            _log.Info($"But the old batch was already spent, so no change warn needed");
                            return null;
                        }
                        else
                        {
                            return $"Pozor, zbývá {oldBatchAmount.ToString()} dříve použité šarže {oldBatchInfo.Item1}!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error while checking batch change", ex);
            }

            return null;
        }

        public Amount GetAvailableAmount(int batchId)
        {
            return _cache.ReadThrough(GetBatchAmountCacheKey(batchId), TimeSpan.FromMinutes(10), () =>
            {
                var result = ExecBatchAmountProcedure(batchId, null).FirstOrDefault();
                
                if (result != null)
                {
                    return new Amount(result.Amount, _unitRepository.GetUnit(result.UnitId));
                }

                var b = _batchRepository.GetBatchById(batchId);
                if (b == null)
                {
                    throw new InvalidOperationException("Invalid entity reference");
                }

                return new Amount(0, b.ComponentUnit);
            });
        }

        public Amount GetAvailableAmount(BatchKey batchKey)
        {
            var batches = _batchRepository.GetBatches(batchKey);
            return _amountProcessor.Sum(batches.Select(b => GetAvailableAmount(b.Id)));
        }



        private static readonly object s_preloadLock = new object();
        public void PreloadBatchAmountCache()
        {
            try
            {
                if (!Monitor.TryEnter(s_preloadLock))
                {
                    return;
                }

                var flag = $"batchAmountCachePreloaded_for_project_{_session.Project.Id}";

                var itsMe = Guid.NewGuid();
                if (!_cache.ReadThrough(flag, TimeSpan.FromMinutes(10), () => itsMe).Equals(itsMe))
                {
                    //it returned another Guid so it was already preloaded in last 10 minutes
                    return;
                }

                var results = ExecBatchAmountProcedure(null, null);

                foreach (var res in results)
                {
                    _cache.ReadThrough(GetBatchAmountCacheKey(res.BatchId), TimeSpan.FromMinutes(10),
                        () => new Amount(res.Amount, _unitRepository.GetUnit(res.UnitId)));
                }
            }
            finally
            {
                Monitor.Exit(s_preloadLock);
            }
        }

        public IEnumerable<OrderItemBatchAssignmentModel> TryResolveBatchAssignments(IPurchaseOrder order, Tuple<long, BatchKey, decimal> orderItemBatchPreference = null)
        {
            return TryResolveBatchAssignmentsPrivate(order, orderItemBatchPreference, false).ToList();
        }

        public BatchKey FindBatchBySearchQuery(int materialId, string query)
        {
            var found = _database.Sql()
                .Call("GetAvailableBatchesByNrLike")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@materialId", materialId)
                .WithParam("@batchNrLike", $"%{query}")
                .MapRows<string>(r => r.GetString(0))
                .Distinct()
                .ToList();

            if (found.Count == 1)
            {
                return new BatchKey(materialId, found[0]);
            }

            if (found.Count > 1)
            {
                throw new InvalidOperationException($"Více než jedna šarže odpovídá zadanému číslu \"{query}\", nalezeno: \"[{string.Join(",",found)}]\" , použijte prosím celé číslo šarže");
            }

            throw new InvalidOperationException($"Šarže \"{query}\" nebyla nalezena");
        }

        public bool AlignOrderBatches(long purchaseOrderId)
        {
            var result = false;

            var preferrences = new HashSet<int>();

            using (var tx = _database.OpenTransaction())
            {
                var order = _orderRepository.GetOrder(purchaseOrderId);
                var allItems = GetAllOrderItems(order);

                foreach (var item in allItems)
                {
                    var assignments = item.AssignedBatches.ToList();
                    
                    if (assignments.Select(a => a.MaterialBatch.BatchNumber).Distinct().Count() == 1)
                    {
                        preferrences.Add(assignments[0].MaterialBatchId);
                        continue;
                    }
                    
                    result = AlignAssignments(assignments) | result;
                }

                foreach (var pref in preferrences)
                {
                    if (GetAvailableAmount(pref).IsPositive)
                    {
                        _batchPreferrenceRepository.SetBatchPreferrence(new BatchKey(pref));
                    }
                }

                tx.Commit();
            }

            return result;
        }

        private bool AlignAssignments(List<IOrderItemMaterialBatch> assignments)
        {
            var result = false;

            var all = new Dictionary<int, List<IOrderItemMaterialBatch>>(assignments.Count);

            foreach (var asig in assignments)
            {
                List<IOrderItemMaterialBatch> thisGroup;
                if (!all.TryGetValue(asig.MaterialBatchId, out thisGroup))
                {
                    thisGroup = new List<IOrderItemMaterialBatch>();
                    all.Add(asig.MaterialBatchId, thisGroup);
                }

                thisGroup.Add(asig);
            }

            while (all.Count > 0)
            {
                var k = all.Keys.First();
                var group = all[k];
                all.Remove(k);

                if (group.Count < 2)
                {
                    continue;
                }

                result = true;

                var sumAsig = group.OrderBy(i => i.AssignmentDt).Last();
                sumAsig.Quantity = group.Sum(g => g.Quantity);

                foreach (var toDel in group.Where(i => i.Id != sumAsig.Id))
                {
                    _database.Delete(toDel);
                }

                _database.Save(sumAsig);
            }

            return result;
        }

        public void ChangeOrderItemBatchAssignment(IPurchaseOrder order,
            long orderItemId,
            string batchNumber,
            decimal? requestNewAmount)
        {
            using (var tx = _database.OpenTransaction())
            {
                var item = GetAllOrderItems(order).FirstOrDefault(i => i.Id == orderItemId);
                if (item == null)
                {
                    throw new InvalidOperationException("Invalid item id");
                }

                var oldAssignments = item.AssignedBatches.Where(a =>
                        a.MaterialBatch.BatchNumber.Equals(batchNumber, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
                if (!oldAssignments.Any())
                {
                    throw new InvalidOperationException("Invalid batch id");
                }
                
                _database.DeleteAll(oldAssignments);
            
                foreach (var oldAssignment in oldAssignments)
                {
                    ReleaseBatchAmountCache(oldAssignment.MaterialBatchId);
                }
                
                tx.Commit();
            }
        }

        public void AssignComponent(int parentBatchId, int componentBatchId, Amount amountToAssign)
        {
            try
            {
                using (var tx = _database.OpenTransaction())
                {
                    var existingComposition =
                        _database.SelectFrom<IMaterialBatchComposition>()
                            .Where(c => (c.ComponentId == componentBatchId) && (c.CompositionId == parentBatchId))
                            .Execute()
                            .FirstOrDefault();

                    if (existingComposition != null)
                    {
                        throw new InvalidOperationException("Redundant composition attempt");
                    }


                    var composition = _database.New<IMaterialBatchComposition>();
                    composition.CompositionId = parentBatchId;
                    composition.ComponentId = componentBatchId;
                    composition.Volume = amountToAssign.Value;
                    composition.UnitId = amountToAssign.Unit.Id;

                    _database.Save(composition);

                    tx.Commit();
                }
            }
            finally
            {
                ReleaseBatchAmountCache(parentBatchId);
                ReleaseBatchAmountCache(componentBatchId);
            }
        }

        public void UnassignComponent(int parentBatchId, int componentBatchId)
        {
            try
            {
                using (var tx = _database.OpenTransaction())
                {
                    var compositions =
                        _database.SelectFrom<IMaterialBatchComposition>()
                            .Where(c => (c.ComponentId == componentBatchId) && (c.CompositionId == parentBatchId))
                            .Execute();

                    _database.DeleteAll(compositions);

                    tx.Commit();
                }
            }
            finally
            {
                ReleaseBatchAmountCache(parentBatchId);
                ReleaseBatchAmountCache(componentBatchId);
            }
        }

        public IEnumerable<Tuple<IMaterialBatch, Amount>> AutoResolve(int materialId, Amount requiredAmount, bool unresolvedAsNullBatch = false, int? batchId = null)
        {
            var batches = new List<MaterialBatchComponent>();

            if (batchId == null)
            {
                batches.AddRange(_batchRepository.GetMaterialBatches(
                    DateTime.Now.AddYears(-1),
                    DateTime.Now.AddYears(1),
                    false,
                    materialId).OrderBy(b => b.Batch.Created));
            }
            else
            {
                var batch = _batchRepository.GetBatchById(batchId.Value).Ensure();
                if (batch.Batch.MaterialId != materialId)
                {
                    throw new InvalidOperationException("Invalid entity reference");
                }

                batches.Add(batch);
            }

            foreach (var batch in batches)
            {
                if (!requiredAmount.IsPositive)
                {
                    yield break;
                }

                var batchAvailableAmount = GetAvailableAmount(batch.Batch.Id);
                if (!batchAvailableAmount.IsPositive)
                {
                    continue;
                }

                var amountToAllocate = _amountProcessor.Min(requiredAmount, batchAvailableAmount);

                yield return new Tuple<IMaterialBatch, Amount>(batch.Batch, amountToAllocate);

                requiredAmount = _amountProcessor.Subtract(requiredAmount, amountToAllocate);
            }

            if (requiredAmount.IsPositive && unresolvedAsNullBatch)
            {
                yield return new Tuple<IMaterialBatch, Amount>(null, requiredAmount);
            }
        }

        public void SetBatchLock(int batchId, bool lockValue, string note)
        {
            using (var tx = _database.OpenTransaction())
            {

                var batch = _batchRepository.GetBatchById(batchId)?.Batch;
                if (batch == null)
                {
                    throw new InvalidOperationException("Pozadovana sarze neni dostupna");
                }

                var isLocked = batch.LockDt != null;
                if (isLocked == lockValue)
                {
                    tx.Commit();
                    return;
                }

                if ((!batch.IsAvailable) && (!lockValue))
                {
                    throw new InvalidOperationException("Nelze odemknout nekompletní šarži");
                }

                batch.LockDt = DateTime.Now;
                batch.LockReason = note ?? string.Empty;

                _database.Save(batch);

                tx.Commit();
            }
        }
        
        public void DeleteBatch(int batchId)
        {
            using (var tx = _database.OpenTransaction())
            {
                var batch = _batchRepository.GetBatchById(batchId);
                if (batch == null)
                {
                    throw new InvalidOperationException("šarže neexistuje");
                }

                var relatedOrder = _orderRepository.GetOrdersByMaterialBatch(batchId).FirstOrDefault(o => !OrderStatus.IsUnsuccessfullyClosed(o.OrderStatusId));
                if (relatedOrder != null)
                {
                    throw new InvalidOperationException($"Není možné smazat šarži, protože byla již použita v objednávce {relatedOrder.OrderNumber}");
                }

                var dependeingBatch = _batchRepository.GetCompositionsByComponentBatchId(batchId).FirstOrDefault();
                if (dependeingBatch != null)
                {
                    var dependingBatchEntity = _batchRepository.GetBatchById(dependeingBatch.CompositionId);
                    throw new InvalidOperationException($"Není možné smazat šarži, protože je již součástí šarže {dependingBatchEntity.Batch.BatchNumber}");
                }

                var toDel = _database.SelectFrom<IMaterialBatchComposition>().Where(c => c.CompositionId == batchId).Execute().ToList();
                
                _database.DeleteAll(toDel);
                _database.Delete(batch.Batch);

                foreach (var compo in toDel)
                {
                    ReleaseBatchAmountCache(compo.ComponentId);
                }

                ReleaseBatchAmountCache(batchId);

                tx.Commit();
            }
        }

        public void DeleteBatch(BatchKey batchKey)
        {
            using (var tx = _database.OpenTransaction())
            {
                foreach (var b in _batchRepository.GetBatches(batchKey))
                {
                    DeleteBatch(b.Id);
                }

                tx.Commit();
            }
        }

        public void ReleaseBatchAmountCache(IMaterialBatch batch)
        {
            ReleaseBatchAmountCache(batch.Id);
        }

        public IEnumerable<string> GetDeletionBlockReasons(int batchId)
        {
            throw new NotImplementedException();
        }
        
        public IEnumerable<MaterialLevelModel> GetMaterialLevels(bool includeUnwatched = false)
        {
            var materialIds = includeUnwatched ? _materialRepository.GetAllMaterials(null, false).Select(m => m.Id) : _materialThresholdRepository.GetAllThresholds().Select(t => t.MaterialId);

            foreach (var materialId in materialIds)
            {
                yield return GetMaterialLevel(materialId);
            }
        }

        public MaterialLevelModel GetMaterialLevel(int materialId)
        {
            PreloadBatchAmountCache();

            return _cache.ReadThrough(GetMaterialLevelCacheKey(materialId), TimeSpan.FromDays(1),
                () =>
                {
                    var material = _materialRepository.GetMaterialById(materialId);
                    if (material == null)
                    {
                        return null;
                    }
                    var amount = new Amount(0, material.NominalUnit);

                    var batches =
                        _batchRepository.GetBatchIds(DateTime.Now.AddYears(-10),
                            DateTime.Now.AddYears(1),
                            materialId:material.Id);

                    foreach (var batchId in batches)
                    {
                        amount = _amountProcessor.Add(amount, GetAvailableAmount(batchId));
                    }
                    
                    var threshold = _materialThresholdRepository.GetThreshold(materialId) ?? CreateFakeThreshold(material, amount);

                    var amountUnitThreshold =
                        _conversionHelper.ConvertAmount(
                            new Amount(threshold.ThresholdQuantity, _unitRepository.GetUnit(threshold.UnitId)),
                            amount.Unit.Id);

                    return new MaterialLevelModel
                    {
                        MaterialId = material.Id,
                        MaterialName = material.Name,
                        ActualValue = amount.Value,
                        MaxValue = amountUnitThreshold.Value * 10m,
                        MinValue = amountUnitThreshold.Value,
                        PercentLevel = GetPercentLevel(amountUnitThreshold.Value * 10m, amount.Value),
                        UnitId = amount.Unit.Id,
                        Unit = amount.Unit.Symbol,
                        HasThreshold = threshold.Id > 0
                    };
                });
        }

        private int GetPercentLevel(decimal full, decimal current)
        {
            if (current < 0.0001m)
            {
                return 0;
            }

            if ((full - current) < 0.0001m)
            {
                return 100;
            }

            return (int)(current / full * 100m);
        }

        private IMaterialThreshold CreateFakeThreshold(IExtendedMaterialModel material, Amount amount)
        {
            return new DefaultThreshold()
            {
                Material = material.Adaptee,
                MaterialId = material.Id,
                Unit = amount.Unit,
                UnitId = amount.Unit.Id,
                ThresholdQuantity = amount.Value * 2m
            };
        }

        public int GetMaterialIdByBatchId(int batchId)
        {
            return _cache.ReadThrough($"batchMaterial{batchId}",
                TimeSpan.FromHours(1), () =>
                _database.SelectFrom<IMaterialBatch>()
                    .Where(b => b.Id == batchId)
                    .Execute()
                    .FirstOrDefault()?.MaterialId ?? -1);
        }

        public BatchEventAmountSuggestions GetEventAmountSuggestions(int eventTypeId, int batchId)
        {
            var eventType = _stockEventRepository.GetAllEventTypes().FirstOrDefault(e => e.Id == eventTypeId);
            if (eventType == null)
            {
                return null;
            }

            var batch = _batchRepository.GetBatchById(batchId);
            if (batch == null)
            {
                return null;
            }
            
            var available = GetAvailableAmount(batchId);
            
            var suggestion = new BatchEventAmountSuggestions(batchId, eventTypeId, available.Value, batch.ComponentUnit.IntegerOnly);

            if (batch.ComponentUnit.IntegerOnly)
            {
                suggestion.AddSuggestion(new Amount(1, batch.ComponentUnit));
                suggestion.AddSuggestion(new Amount(5, batch.ComponentUnit));
                suggestion.AddSuggestion(new Amount(10, batch.ComponentUnit));
                suggestion.AddSuggestion(new Amount(100, batch.ComponentUnit));
            }
            
            suggestion.AddSuggestion(available);
            
            return suggestion;
        }

        public IEnumerable<IMaterialBatch> FindBatchesWithMissingInvoiceItem(int inventoryId)
        {
            var ids = _database.Sql().ExecuteWithParams(@"SELECT DISTINCT mb.Id
            FROM MaterialBatch mb
                INNER JOIN Material      m ON(m.Id = mb.MaterialId)
            WHERE mb.ProjectId = {0}
            AND m.InventoryId = {1}
            AND NOT EXISTS(SELECT TOP 1 1
            FROM InvoiceFormItemMaterialBatch ib
                WHERE ib.MaterialBatchId = mb.Id)",
                _session.Project.Id,
                inventoryId).MapRows(r => r.GetInt32(0));

            return ids.Select(id => _batchRepository.GetBatchById(id).Batch);
        }

        public IEnumerable<IMaterialBatch> FindNotClosedBatches(int inventoryId, DateTime from, DateTime to, Func<IMaterialBatch, bool> filter = null)
        {
            return _batchRepository
                .QueryBatches(q =>
                    q.Join(b => b.Material).Where(b => b.Material.InventoryId == inventoryId)
                        .Where(b => (b.Created >= from) && (b.Created < to)).Where(b => b.CloseDt == null)).Where(b => filter?.Invoke(b) != false)
                .Select(b => _batchRepository.GetBatchById(b.Id).Batch);
        }
        
        public BatchAccountingDate GetBatchAccountingDate(IMaterialBatch batch)
        {
           return new BatchAccountingDate(batch.Created);
        }
        
        public BatchPrice GetBatchPrice(IMaterialBatch batch, IBatchPriceBulkProvider provider)
        {
            var compos = provider.GetBatchPriceComponents(batch.Id);
            
            var bp = new BatchPrice(batch);

            foreach (var c in compos)
            {
                bp.AddComponent(c.IsWarning, c.SourceBatchId, c.Text, c.RawValue);
            }

            return bp;
        }

        private string GetBatchStatusCacheKey(int batchId)
        {
            return $"btchstat_{batchId}";
        }

        private string GetBatchAmountCacheKey(int batchId)
        {
            return $"btchamnt_{batchId}";
        }

        private string GetMaterialLevelCacheKey(int materialId)
        {
            return $"mrlvl_{materialId}";
        }

        public void ReleaseBatchAmountCache(int batchId)
        {
            var materialId = GetMaterialIdByBatchId(batchId);
            
            _cache.Remove(GetMaterialLevelCacheKey(materialId));
            _cache.Remove(GetBatchStatusCacheKey(batchId));
            _cache.Remove(GetBatchAmountCacheKey(batchId));

            InvalidateBatchesDependantCaches();
        }

        private List<IOrderItem> GetAllOrderItems(IPurchaseOrder order)
        {
            var items = new List<IOrderItem>(order.Items.Count() * 2);

            foreach (var orderItem in order.Items)
            {
                var kitItems = _kitProductRepository.GetKitForOrderItem(order, orderItem).ToList();

                if (!kitItems.Any())
                {
                    items.Add(orderItem);
                    continue;
                }

                foreach (var child in orderItem.KitChildren)
                {
                    items.Add(child);
                }

                foreach (var kitItem in kitItems)
                {
                    if (kitItem.SelectedItem != null)
                    {
                        if (items.All(i => i.Id != kitItem.SelectedItem.Id))
                        {
                            items.Add(kitItem.SelectedItem);
                        }
                    }
                }
            }
            return items;
        }

        private IEnumerable<OrderItemBatchAssignmentModel> TryResolveBatchAssignmentsPrivate(IPurchaseOrder order, Tuple<long, BatchKey, decimal> orderItemBatchPreference, bool doNotRetry)
        {
            var items = GetAllOrderItems(order);

            var result = new List<OrderItemBatchAssignmentModel>(items.Count * 2);
            
            foreach (var orderItem in items)
            {
                var material = _virtualProductFacade.GetOrderItemMaterialForSingleUnit(order, orderItem);

                var adHocPreferrence = orderItemBatchPreference;
                if (adHocPreferrence?.Item1 != orderItem.Id)
                {
                    adHocPreferrence = null;
                }

                var amountToAllocate = new Amount(orderItem.Quantity, material.Amount.Unit);

                //1. already assigned batches:
                foreach (var assignment in orderItem.AssignedBatches)
                {
                    var assignedAmount = new Amount(assignment.Quantity, material.Amount.Unit);
                    amountToAllocate = _amountProcessor.Subtract(amountToAllocate, assignedAmount);

                    result.Add(new OrderItemBatchAssignmentModel()
                    {
                        AssignedQuantity = assignment.Quantity,
                        MaterialBatchId = assignment.MaterialBatchId,
                        BatchNumber = _batchRepository.GetBatchById(assignment.MaterialBatchId)?.Batch?.BatchNumber,
                        OrderItemId = orderItem.Id,
                    });
                }

                if (amountToAllocate.IsZero)
                {
                    // We already allocated the whole quantity
                    continue;
                }

                //2. assignment by preferrence
                var preferredBatchNumber = _batchPreferrenceRepository.GetPrefferedBatchNumber(material.MaterialId);

                if (adHocPreferrence != null)
                {
                    preferredBatchNumber = adHocPreferrence.Item2.GetBatchNumber(_batchRepository);
                }

                if (preferredBatchNumber != null)
                {
                    var preferredBatchesKey = new BatchKey(material.MaterialId, preferredBatchNumber);
                    var preferredBatches = _batchRepository.GetBatches(preferredBatchesKey).ToList();

                    foreach (var preferredBatch in preferredBatches)
                    {
                        var availableBatchAmount = GetAvailableAmount(preferredBatch.Id);
                        if (availableBatchAmount.Value > 0m)
                        {
                            var allocateByPreferrence = _amountProcessor.Min(amountToAllocate, availableBatchAmount);

                            if (adHocPreferrence != null)
                            {
                                allocateByPreferrence =
                                    _amountProcessor.Min(
                                        new Amount(adHocPreferrence.Item3, material.Amount.Unit),
                                        allocateByPreferrence);
                            }

                            amountToAllocate = _amountProcessor.Subtract(amountToAllocate, allocateByPreferrence);

                            var assignment = new OrderItemBatchAssignmentModel()
                            {
                                AssignedQuantity = allocateByPreferrence.Value,
                                MaterialBatchId = preferredBatch.Id,
                                BatchNumber = preferredBatch.BatchNumber,
                                OrderItemId = orderItem.Id
                            };

                            result.Add(assignment);

                            AssignOrderItemToBatch(preferredBatch.Id, order, orderItem.Id, allocateByPreferrence.Value, out var batchChangeWarnMessage);
                            assignment.WarningMessage = batchChangeWarnMessage;                            
                        }

                        if (availableBatchAmount.IsNotPositive)
                        {
                            _batchPreferrenceRepository.InvalidatePreferrenceByMaterialId(material.MaterialId);
                        }
                    }
                }

                if (amountToAllocate.IsZero)
                {
                    // We already allocated the whole quantity
                    continue;
                }

                // 3. unable to allocate the whole amount
                result.Add(new OrderItemBatchAssignmentModel()
                {
                    AssignedQuantity = amountToAllocate.Value,
                    MaterialBatchId = -1,
                    OrderItemId = orderItem.Id
                });
            }

            if (AlignOrderBatches(order.Id))
            {
                if (doNotRetry)
                {
                    _log.Error("Second attempt to AlignOrderBatches failed");
                }
                else
                {
                    return TryResolveBatchAssignmentsPrivate(_orderRepository.GetOrder(order.Id), orderItemBatchPreference, true).ToList();
                }
            }

            foreach(var assignment in result)
            {
                if (assignment.MaterialBatchId == -1)
                    continue;

                var batch = _batchRepository.GetBatchById(assignment.MaterialBatchId);
                if (batch == null)
                    continue;

                var material = batch.Batch.Material ?? _materialRepository.GetMaterialById(batch.Batch.MaterialId)?.Adaptee;

                if (material.ExpirationMonths != null)
                {
                    var customer = _customerRepository.GetCustomerByErpUid(order.CustomerErpUid);
                    var isDistributor = (customer?.IsDistributor == true);

                    var expDate = batch.Batch.Created.AddMonths(material.ExpirationMonths.Value);

                    var monthsToExpiration = DateUtil.GetRemainingMonths(expDate);

                    var expLimit = (isDistributor ? (material.DistributorExpirationLimit ?? material.RetailExpirationLimit) : (material.RetailExpirationLimit ?? material.DistributorExpirationLimit)) ?? 0;

                    if (monthsToExpiration <= expLimit)
                    {
                        assignment.PinnedWarningMessage = $"Zbývá {monthsToExpiration} měsíců do expirace. Limit pro {(isDistributor ? "velkoodběratele" : "maloodběratele")} je {expLimit} měs.";
                    }
                }
            }

            return result;
        }

        public Tuple<decimal, BatchPrice> GetPriceOfAmount(IMaterialBatch batch, Amount amount, IBatchPriceBulkProvider provider)
        {
            var batchPrice = GetBatchPrice(batch, provider);

            var totalAmount = _conversionHelper.ConvertAmount(new Amount(batchPrice.Batch.Volume, _unitRepository.GetUnit(batchPrice.Batch.UnitId)), amount.Unit.Id);
            var pricePerUnit = batchPrice.TotalPriceInPrimaryCurrency / totalAmount.Value;

            return new Tuple<decimal, BatchPrice>(pricePerUnit * amount.Value, batchPrice);
        }

        public Amount GetNumberOfProducedProducts(int accountingDateYear, int accountingDateMonth, int inventoryId)
        {
            var allBatches = _database.SelectFrom<IMaterialBatch>().Join(b => b.Material)
                .Where(b => b.ProjectId == _session.Project.Id).Where(b => b.CloseDt == null)
                .Where(b => b.Material.InventoryId == inventoryId).Execute();

            return _amountProcessor.Sum(allBatches.Where(b =>
            {
                var ad = GetBatchAccountingDate(b);
                
                return ad.IsFinal &&
                       (ad.AccountingDate.Year == accountingDateYear) &&
                       (ad.AccountingDate.Month == accountingDateMonth);
            }).Select(b => new Amount(b)));
        }

        public void ReleaseUnsentOrdersAllocations()
        {
            var releasedBatches = new List<int>();
            _database.Sql()
                .Call("sp_deallocateUnpackOrderBatches")
                .ReadRows<int>(i => releasedBatches.Add(i));

            foreach (var batchId in releasedBatches)
            {
                ReleaseBatchAmountCache(batchId);
            }
        }

        /// <summary>
        /// Use key = null to delete all allocations from this order
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="key"></param>
        public void CutOrderAllocation(long orderId, BatchKey key)
        {
            var order = _orderRepository.GetOrder(orderId).Ensure();

            if (OrderStatus.IsSent(order.OrderStatusId))
            {
                throw new InvalidOperationException("Tato objednávka již byla odeslána");
            }

            var assignmentsToCut = new List<IOrderItemMaterialBatch>();

            foreach (var item in order.Items)
            {
                foreach (var orderItemMaterialBatch in item.AssignedBatches.Where(i => key?.Match(i.MaterialBatch, this) ?? true))
                {
                    assignmentsToCut.Add(orderItemMaterialBatch);
                }

                foreach (var kitChild in item.KitChildren)
                {
                    foreach (var orderItemMaterialBatch in kitChild.AssignedBatches.Where(i => key?.Match(i.MaterialBatch, this) ?? true))
                    {
                        assignmentsToCut.Add(orderItemMaterialBatch);
                    }
                }
            }
            
            if (!assignmentsToCut.Any())
            {
                _log.Error($"CutOrderAllocation - no allocations found orderId={orderId}, key={key?.ToString(this)}");
            }

            _database.DeleteAll(assignmentsToCut);

            foreach (var cutBatchId in assignmentsToCut.Select(a => a.MaterialBatchId).Distinct())
            {
                ReleaseBatchAmountCache(cutBatchId);
            }
        }

        public IEnumerable<Tuple<int?, Amount>> ProposeAllocations(int materialId, string batchNumber, Amount requestedAmount)
        {
            var query = _database.SelectFrom<IMaterialBatch>()
                .Where(b => b.CloseDt == null)
                .Where(b => b.ProjectId == _session.Project.Id)
                .Where(b => b.MaterialId == materialId)
                .OrderBy(b => b.Created);

            if (string.IsNullOrWhiteSpace(batchNumber))
            {
                var material = _materialRepository.GetMaterialById(materialId).Ensure();
                if (!material.AutomaticBatches)
                {
                    throw new InvalidOperationException("Chybi cislo sarze");
                }
            }
            else
            {
                query = query.Where(b => b.BatchNumber == batchNumber);
            }

            var batches = query.Execute().ToList();

            foreach (var b in batches)
            {
                if (!requestedAmount.IsPositive)
                {
                    break;
                }

                var available = GetAvailableAmount(b.Id);

                if (!available.IsPositive)
                {
                    continue;
                }

                var allocatedFromThisBatch = _amountProcessor.Min(available, requestedAmount);

                requestedAmount = _amountProcessor.Subtract(requestedAmount, allocatedFromThisBatch);

                yield return new Tuple<int?, Amount>(b.Id, allocatedFromThisBatch);
            }

            if (requestedAmount.IsPositive)
            {
                yield return new Tuple<int?, Amount>(null, requestedAmount);
            }
        }

        public AllocationRequestResult ResolveMaterialDemand(int materialId,
            Amount demand,
            string batchNumberOrNull,
            bool batchNumberIsPreferrence,
            bool includeBatchesWithoutAllocation, 
            DateTime? batchesProducedBefore = null, 
            int? ignoreExistenceOfBatchId = null)
        {
            var batchIdNumberAmount = new List<Tuple<int, string, Amount>>();

            var batchCreated = new Dictionary<string, DateTime>();

            _database.Sql().Call("GetMaterialResolution")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@materialId", materialId)
                .WithParam("@createdBefore", batchesProducedBefore)
                .WithParam("@ignoreExistenceOfThisBatch", ignoreExistenceOfBatchId)
                .ReadRows<int, string, decimal, int, DateTime>(
                    (batchId, batchNumber, available, unitId, created) =>
                    {
                        if (!string.IsNullOrWhiteSpace(batchNumberOrNull) && (!batchNumberIsPreferrence) &&
                            !batchNumberOrNull.Equals(batchNumber, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return;
                        }

                        if (!batchCreated.TryGetValue(batchNumber.ToLowerInvariant(), out var dateA))
                        {
                            batchCreated[batchNumber.ToLowerInvariant()] = created;
                        }
                        else
                        {
                            batchCreated[batchNumber.ToLowerInvariant()] =  dateA > created ? dateA : created;
                        }

                        batchIdNumberAmount.Add(new Tuple<int, string, Amount>(batchId, batchNumber, new Amount(available, _unitRepository.GetUnit(unitId))));
                    });

            if (!string.IsNullOrWhiteSpace(batchNumberOrNull) && batchNumberIsPreferrence)
            {
                batchIdNumberAmount = batchIdNumberAmount.OrderByDescending(r =>
                    r.Item2.Equals(batchNumberOrNull, StringComparison.InvariantCultureIgnoreCase) ? 1 : 0).ToList();
            }

            var batchIdNumberTotalAllocated = new List<Tuple<int, string, Amount, Amount>>(batchIdNumberAmount.Count);

            foreach (var src in batchIdNumberAmount)
            {
                var canAllocateToThisBatch = _amountProcessor.Min(demand, src.Item3);
                batchIdNumberTotalAllocated.Add(new Tuple<int, string, Amount, Amount>(src.Item1, src.Item2, src.Item3, canAllocateToThisBatch));

                demand = _amountProcessor.Subtract(demand, canAllocateToThisBatch);
                if ((!includeBatchesWithoutAllocation) && (!demand.IsPositive))
                {
                    break;
                }
            }

            var result = new AllocationRequestResult(materialId, demand.IsNotPositive, _amountProcessor.Sum(batchIdNumberTotalAllocated.Select(a => a.Item4)), demand);

            foreach (var batchNumber in batchIdNumberTotalAllocated.Select(b => b.Item2).Distinct())
            {
                var thisBatchNrAllocations = batchIdNumberTotalAllocated
                    .Where(a => a.Item2.Equals(batchNumber, StringComparison.InvariantCultureIgnoreCase)).ToList();

                batchCreated.TryGetValue(batchNumber.ToLowerInvariant(), out var dt);

                result.Allocations.Add(new BatchAllocation(
                    batchNumber, 
                    _amountProcessor.Sum(thisBatchNrAllocations.Select(a => a.Item3)),
                    _amountProcessor.Sum(thisBatchNrAllocations.Select(a => a.Item4)),
                    thisBatchNrAllocations.Select(a => new Tuple<int, Amount>(a.Item1, a.Item4)).ToList()
                    , dt));
            }

            return result;
        }

        public IEnumerable<PriceComponentModel> GetPriceComponents(int batchId, bool addSum = true)
        {
            var result = new List<PriceComponentModel>();

            _database.Sql().Call("GetBatchPriceComponents")
                .WithParam("@batchId", batchId)
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@culture", _session.Culture)
                .ReadRows<int, string, decimal, bool, int?>((bId, txt, val, wrn, sourceBatchId) => result.Add(new PriceComponentModel(bId)
                {
                    IsWarning = wrn,
                    RawValue = val,
                    Text = txt,
                    SourceBatchId = sourceBatchId
                }));

            if (addSum)
            {
                AddSumPriceComponent(result);
            }

            return result;
        }

        public List<PriceComponentModel> GetBatchPriceComponents(int batchId)
        {
            return GetPriceComponents(batchId, false).ToList();
        }

        private static void AddSumPriceComponent(List<PriceComponentModel> result)
        {
            var fb = result.FirstOrDefault();
            if (fb != null)
            {
                result.Add(new PriceComponentModel(fb.BatchId)
                {
                    Text = "SUMA",
                    RawValue = result.Sum(r => r.RawValue)
                });
            }
        }

        public IEnumerable<PriceComponentModel> GetPriceComponents(BatchKey key)
        {
            var batches = _batchRepository.GetBatches(key).Select(b => b.Id);

            var result = new List<PriceComponentModel>();

            foreach (var bid in batches)
            {
                var segmentComponents = GetPriceComponents(bid, false);

                foreach (var sc in segmentComponents)
                {
                    var existing = result.FirstOrDefault(c =>
                        c.Text.Equals(sc.Text, StringComparison.InvariantCultureIgnoreCase));
                    if (existing != null)
                    {
                        existing.RawValue += sc.RawValue;
                        existing.IsWarning = existing.IsWarning || sc.IsWarning;
                        continue;
                    }

                    result.Add(sc);
                }
            }

            var warnings = new List<PriceComponentModel>();

            for (var i = result.Count - 1; i >= 0; i--)
            {
                if (result[i].IsWarning)
                {
                    warnings.Insert(0, result[i]);
                    result.RemoveAt(i);
                }
            }

            result.AddRange(warnings);

            AddSumPriceComponent(result);

            return result;
        }

        public IBatchPriceBulkProvider CreatPriceBulkProvider(DateTime from, DateTime to)
        {
            return new BatchPriceBulkProvider(_database, _session, this, from, to);
        }

        private IList<BatchAmountProcedureResult> ExecBatchAmountProcedure(int? batchId, int? materialId)
        {
            var result = new List<BatchAmountProcedureResult>();

            _database.Sql().Call("CalculateBatchUsages")
                .WithParam("@ProjectId", _session.Project.Id)
                .WithParam("@batchId", batchId)
                .WithParam("@materialId", materialId)
                .ReadRows<int, int, decimal>((bId, unitId, amount) =>
                    result.Add(new BatchAmountProcedureResult(bId, unitId, amount)));

            return result;
        }

        #region Nested
        private class DefaultThreshold : IMaterialThreshold
        {
            public int ProjectId { get; set; }

            public IProject Project { get; } = null;

            public int Id { get; } = -1;

            public int MaterialId { get; set; }

            public IMaterial Material { get; set; }

            public decimal ThresholdQuantity { get; set; }

            public int UnitId { get; set; }

            public IMaterialUnit Unit { get; set; }

            public DateTime UpdateDt { get; set; }

            public int UpdateUserId { get; set; }

            public IUser UpdateUser { get; } = null;
        }

        private sealed class BatchAmountProcedureResult
        {
            public readonly int BatchId;
            public readonly int UnitId;
            public readonly decimal Amount;

            public BatchAmountProcedureResult(int batchId, int unitId, decimal amount)
            {
                BatchId = batchId;
                UnitId = unitId;
                Amount = amount;
            }
        }

        #endregion

        public Tuple<int, string> GetBatchNumberAndMaterialIdByBatchId(int batchId)
        {
            return _batchRepository.GetBatchNumberAndMaterialIdByBatchId(batchId);
        }

        private bool CheckBatchChange(int materialId, int newBatchId, out Tuple<string, int> oldBatch)
        {
            var ckey = $"batchAssignments_{_session.Project.Id}";
            //var currentAssignments = m_cache.ReadThrough<Dictionary<int, Tuple<string, int>>>(ckey, TimeSpan.FromHours(1),
            //    () => LoadCurrentBatchAssignments());

            var currentAssignments = LoadCurrentBatchAssignments();

            if ((!currentAssignments.TryGetValue(materialId, out oldBatch)) || (oldBatch.Item2 != newBatchId))
            {
                _log.Info($"Batch assignment change - now using '{newBatchId}' instead of previously used '{oldBatch?.Item2}'. (MaterialId={materialId})");
                _cache.Remove(ckey);
                return true;
            }

            return false;
        }

        private Dictionary<int, Tuple<string, int>> LoadCurrentBatchAssignments()
        {
            var res = new Dictionary<int, Tuple<string, int>>();

            _database.Sql().Execute(@"SELECT mb.MaterialId, mb.BatchNumber, mb.Id BatchId
                                          FROM OrderItemMaterialBatch oimb
                                          JOIN MaterialBatch mb ON (oimb.MaterialBatchId = mb.Id)
                                          JOIN (SELECT  
		                                        mb.MaterialId, MAX(oimb.AssignmentDt) lastAss
		                                          FROM OrderItemMaterialBatch oimb
		                                          JOIN MaterialBatch mb ON (oimb.MaterialBatchId = mb.Id)
		                                         GROUP BY mb.MaterialId) la ON (la.MaterialId = mb.MaterialId AND la.lastAss = oimb.AssignmentDt)
                                         WHERE mb.ProjectId = @projectId")
                .WithParam("@projectId", _session.Project.Id)
                .ReadRows<int, string, int>((materialId, batchNumber, batchId) => res[materialId] = new Tuple<string, int>(batchNumber, batchId));

            return res;
        }

        public IEnumerable<OneClickProductionOption> GetOneClickProductionOptions()
        {
            var unfilteredOptions = LoadOneClickProdOptions();
            var userRecipes = _recipeRepository.GetRecipes();

            foreach (var option in unfilteredOptions)
            {            
                if (userRecipes.Any(r => r.RecipeId == option.RecipeId))
                {
                    yield return option;
                }
            }
        }

        private List<OneClickProductionOption> LoadOneClickProdOptions()
        {
            var cacheKey = CreateCacheKeyDependantOnBatches("OneClickProductionOptions");

            return _cache.ReadThrough(cacheKey, TimeSpan.FromSeconds(10), () =>  
                _database.Sql().Call("GetOneClickProductionOptions")
                    .WithParam("@projectId", _session.Project.Id)
                    .AutoMap<OneClickProductionOption>());
        }

        public string CreateCacheKeyDependantOnBatches(string suffix)
        {
            return $"p{_session.Project.Id}_dependsOnBatches::{suffix}";
        }

        public void InvalidateBatchesDependantCaches()
        {
            _cache.RemoveByPrefix(CreateCacheKeyDependantOnBatches(string.Empty));
        }
    }
}
