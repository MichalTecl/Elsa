using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Model.BatchPriceExpl;
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

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Impl
{
    public class MaterialBatchFacade : IMaterialBatchFacade, IBatchPriceBulkProvider
    {
        private readonly ILog m_log;
        private readonly IVirtualProductFacade m_virtualProductFacade;
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly AmountProcessor m_amountProcessor;
        private readonly ICache m_cache;
        private readonly IDatabase m_database;
        private readonly IPackingPreferredBatchRepository m_batchPreferrenceRepository;
        private readonly IKitProductRepository m_kitProductRepository;
        private readonly IUnitConversionHelper m_conversionHelper;
        private readonly IMaterialThresholdRepository m_materialThresholdRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly IStockEventRepository m_stockEventRepository;
        private readonly ISession m_session;
        private readonly IFixedCostRepository m_fixedCostRepository;

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
            IFixedCostRepository fixedCostRepository)
        {
            m_log = log;
            m_virtualProductFacade = virtualProductFacade;
            m_batchRepository = batchRepository;
            m_orderRepository = orderRepository;
            m_amountProcessor = amountProcessor;
            m_cache = cache;
            m_database = database;
            m_batchPreferrenceRepository = batchPreferrenceRepository;
            m_kitProductRepository = kitProductRepository;
            m_conversionHelper = conversionHelper;
            m_materialThresholdRepository = materialThresholdRepository;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_stockEventRepository = stockEventRepository;
            m_session = session;
            m_fixedCostRepository = fixedCostRepository;
        }

        public void AssignOrderItemToBatch(int batchId, IPurchaseOrder order, long orderItemId, decimal assignmentQuantity)
        {
            var orderItem = GetAllOrderItems(order).FirstOrDefault(i => i.Id == orderItemId);
            if (orderItem == null)
            {
                throw new InvalidOperationException("Invalid OrderItemId");
            }

            var material = m_virtualProductFacade.GetOrderItemMaterialForSingleUnit(order, orderItem);
            
            var batch = m_batchRepository.GetBatchById(batchId);
            if (batch == null)
            {
                throw new InvalidOperationException("Invalid batch reference");
            }

            if (material.MaterialId != batch.Batch.MaterialId)
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

            using (var tx = m_database.OpenTransaction())
            {
                var available = GetAvailableAmount(batchId);

                var subtracted = m_amountProcessor.Subtract(
                    available,
                    new Amount(assignmentQuantity, material.Amount.Unit));

                if (subtracted.Value < 0m)
                {
                    throw new InvalidOperationException($"Požadované množství {new Amount(assignmentQuantity, material.Amount.Unit)} již není k dispozici v šarži {batch.Batch.BatchNumber}");
                }
                
                m_orderRepository.UpdateOrderItemBatch(orderItem, batchId, assignmentQuantity);
                
                tx.Commit();

                ReleaseBatchAmountCache(batchId);
            }
        }
        
        public Amount GetAvailableAmount(int batchId)
        {
            return m_cache.ReadThrough(GetBatchAmountCacheKey(batchId), TimeSpan.FromMinutes(10), () =>
            {
                var result = ExecBatchAmountProcedure(batchId, null).FirstOrDefault();
                
                if (result != null)
                {
                    return new Amount(result.Amount, m_unitRepository.GetUnit(result.UnitId));
                }

                var b = m_batchRepository.GetBatchById(batchId);
                if (b == null)
                {
                    throw new InvalidOperationException("Invalid entity reference");
                }

                return new Amount(0, b.ComponentUnit);
            });
        }

        public Amount GetAvailableAmount(BatchKey batchKey)
        {
            var batches = m_batchRepository.GetBatches(batchKey);
            return m_amountProcessor.Sum(batches.Select(b => GetAvailableAmount(b.Id)));
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

                var flag = $"batchAmountCachePreloaded_for_project_{m_session.Project.Id}";

                var itsMe = Guid.NewGuid();
                if (!m_cache.ReadThrough(flag, TimeSpan.FromMinutes(10), () => itsMe).Equals(itsMe))
                {
                    //it returned another Guid so it was already preloaded in last 10 minutes
                    return;
                }

                var results = ExecBatchAmountProcedure(null, null);

                foreach (var res in results)
                {
                    m_cache.ReadThrough(GetBatchAmountCacheKey(res.BatchId), TimeSpan.FromMinutes(10),
                        () => new Amount(res.Amount, m_unitRepository.GetUnit(res.UnitId)));
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
            var batches = m_batchRepository.GetMaterialBatches(
                DateTime.Now.AddDays(-365),
                DateTime.Now.AddDays(365),
                false,
                materialId);

            var found = batches.Where(b => b.Batch.BatchNumber.EndsWith(query)).Select(b => b.Batch.BatchNumber).Distinct().ToList();

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

            using (var tx = m_database.OpenTransaction())
            {
                var order = m_orderRepository.GetOrder(purchaseOrderId);
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
                        m_batchPreferrenceRepository.SetBatchPreferrence(new BatchKey(pref));
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
                    m_database.Delete(toDel);
                }

                m_database.Save(sumAsig);
            }

            return result;
        }

        public void ChangeOrderItemBatchAssignment(IPurchaseOrder order,
            long orderItemId,
            string batchNumber,
            decimal? requestNewAmount)
        {
            using (var tx = m_database.OpenTransaction())
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
                
                //m_batchPreferrenceRepository.RemoveBatchFromPreferrence(batchNumber);
                
                m_database.DeleteAll(oldAssignments);
            
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
                using (var tx = m_database.OpenTransaction())
                {
                    var existingComposition =
                        m_database.SelectFrom<IMaterialBatchComposition>()
                            .Where(c => (c.ComponentId == componentBatchId) && (c.CompositionId == parentBatchId))
                            .Execute()
                            .FirstOrDefault();

                    if (existingComposition != null)
                    {
                        throw new InvalidOperationException("Redundant composition attempt");
                    }


                    var composition = m_database.New<IMaterialBatchComposition>();
                    composition.CompositionId = parentBatchId;
                    composition.ComponentId = componentBatchId;
                    composition.Volume = amountToAssign.Value;
                    composition.UnitId = amountToAssign.Unit.Id;

                    m_database.Save(composition);

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
                using (var tx = m_database.OpenTransaction())
                {
                    var compositions =
                        m_database.SelectFrom<IMaterialBatchComposition>()
                            .Where(c => (c.ComponentId == componentBatchId) && (c.CompositionId == parentBatchId))
                            .Execute();

                    m_database.DeleteAll(compositions);

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
                batches.AddRange(m_batchRepository.GetMaterialBatches(
                    DateTime.Now.AddYears(-1),
                    DateTime.Now.AddYears(1),
                    false,
                    materialId).OrderBy(b => b.Batch.Created));
            }
            else
            {
                var batch = m_batchRepository.GetBatchById(batchId.Value).Ensure();
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

                var amountToAllocate = m_amountProcessor.Min(requiredAmount, batchAvailableAmount);

                yield return new Tuple<IMaterialBatch, Amount>(batch.Batch, amountToAllocate);

                requiredAmount = m_amountProcessor.Subtract(requiredAmount, amountToAllocate);
            }

            if (requiredAmount.IsPositive && unresolvedAsNullBatch)
            {
                yield return new Tuple<IMaterialBatch, Amount>(null, requiredAmount);
            }
        }

        public void SetBatchLock(int batchId, bool lockValue, string note)
        {
            using (var tx = m_database.OpenTransaction())
            {

                var batch = m_batchRepository.GetBatchById(batchId)?.Batch;
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

                m_database.Save(batch);

                tx.Commit();
            }
        }
        
        public void DeleteBatch(int batchId)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var batch = m_batchRepository.GetBatchById(batchId);
                if (batch == null)
                {
                    throw new InvalidOperationException("šarže neexistuje");
                }

                var relatedOrder = m_orderRepository.GetOrdersByMaterialBatch(batchId).FirstOrDefault(o => !OrderStatus.IsUnsuccessfullyClosed(o.OrderStatusId));
                if (relatedOrder != null)
                {
                    throw new InvalidOperationException($"Není možné smazat šarži, protože byla již použita v objednávce {relatedOrder.OrderNumber}");
                }

                var dependeingBatch = m_batchRepository.GetCompositionsByComponentBatchId(batchId).FirstOrDefault();
                if (dependeingBatch != null)
                {
                    var dependingBatchEntity = m_batchRepository.GetBatchById(dependeingBatch.CompositionId);
                    throw new InvalidOperationException($"Není možné smazat šarži, protože je již součástí šarže {dependingBatchEntity.Batch.BatchNumber}");
                }

                var toDel = m_database.SelectFrom<IMaterialBatchComposition>().Where(c => c.CompositionId == batchId).Execute().ToList();
                
                m_database.DeleteAll(toDel);
                m_database.Delete(batch.Batch);

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
            using (var tx = m_database.OpenTransaction())
            {
                foreach (var b in m_batchRepository.GetBatches(batchKey))
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
            var materialIds = includeUnwatched ? m_materialRepository.GetAllMaterials(null).Select(m => m.Id) : m_materialThresholdRepository.GetAllThresholds().Select(t => t.MaterialId);

            foreach (var materialId in materialIds)
            {
                yield return GetMaterialLevel(materialId);
            }
        }

        public MaterialLevelModel GetMaterialLevel(int materialId)
        {
            PreloadBatchAmountCache();

            return m_cache.ReadThrough(GetMaterialLevelCacheKey(materialId), TimeSpan.FromDays(1),
                () =>
                {
                    var material = m_materialRepository.GetMaterialById(materialId);
                    if (material == null)
                    {
                        return null;
                    }
                    var amount = new Amount(0, material.NominalUnit);

                    var batches =
                        m_batchRepository.GetBatchIds(DateTime.Now.AddYears(-10),
                            DateTime.Now.AddYears(1),
                            materialId:material.Id);

                    foreach (var batchId in batches)
                    {
                        amount = m_amountProcessor.Add(amount, GetAvailableAmount(batchId));
                    }
                    
                    var threshold = m_materialThresholdRepository.GetThreshold(materialId) ?? CreateFakeThreshold(material, amount);

                    var amountUnitThreshold =
                        m_conversionHelper.ConvertAmount(
                            new Amount(threshold.ThresholdQuantity, m_unitRepository.GetUnit(threshold.UnitId)),
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
            return m_cache.ReadThrough($"batchMaterial{batchId}",
                TimeSpan.FromHours(1), () =>
                m_database.SelectFrom<IMaterialBatch>()
                    .Where(b => b.Id == batchId)
                    .Execute()
                    .FirstOrDefault()?.MaterialId ?? -1);
        }

        public BatchEventAmountSuggestions GetEventAmountSuggestions(int eventTypeId, int batchId)
        {
            var eventType = m_stockEventRepository.GetAllEventTypes().FirstOrDefault(e => e.Id == eventTypeId);
            if (eventType == null)
            {
                return null;
            }

            var batch = m_batchRepository.GetBatchById(batchId);
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
            var ids = m_database.Sql().ExecuteWithParams(@"SELECT DISTINCT mb.Id
            FROM MaterialBatch mb
                INNER JOIN Material      m ON(m.Id = mb.MaterialId)
            WHERE mb.ProjectId = {0}
            AND m.InventoryId = {1}
            AND NOT EXISTS(SELECT TOP 1 1
            FROM InvoiceFormItemMaterialBatch ib
                WHERE ib.MaterialBatchId = mb.Id)",
                m_session.Project.Id,
                inventoryId).MapRows(r => r.GetInt32(0));

            return ids.Select(id => m_batchRepository.GetBatchById(id).Batch);
        }

        public IEnumerable<IMaterialBatch> FindNotClosedBatches(int inventoryId, DateTime from, DateTime to, Func<IMaterialBatch, bool> filter = null)
        {
            return m_batchRepository
                .QueryBatches(q =>
                    q.Join(b => b.Material).Where(b => b.Material.InventoryId == inventoryId)
                        .Where(b => (b.Created >= from) && (b.Created < to)).Where(b => b.CloseDt == null)).Where(b => filter?.Invoke(b) != false)
                .Select(b => m_batchRepository.GetBatchById(b.Id).Batch);
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
            
            m_cache.Remove(GetMaterialLevelCacheKey(materialId));
            m_cache.Remove(GetBatchStatusCacheKey(batchId));
            m_cache.Remove(GetBatchAmountCacheKey(batchId));
        }

        private List<IOrderItem> GetAllOrderItems(IPurchaseOrder order)
        {
            var items = new List<IOrderItem>(order.Items.Count() * 2);

            foreach (var orderItem in order.Items)
            {
                var kitItems = m_kitProductRepository.GetKitForOrderItem(order, orderItem).ToList();

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
                var material = m_virtualProductFacade.GetOrderItemMaterialForSingleUnit(order, orderItem);

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
                    amountToAllocate = m_amountProcessor.Subtract(amountToAllocate, assignedAmount);

                    result.Add(new OrderItemBatchAssignmentModel()
                    {
                        AssignedQuantity = assignment.Quantity,
                        MaterialBatchId = assignment.MaterialBatchId,
                        BatchNumber = m_batchRepository.GetBatchById(assignment.MaterialBatchId)?.Batch?.BatchNumber,
                        OrderItemId = orderItem.Id,
                    });
                }

                if (amountToAllocate.IsZero)
                {
                    // We already allocated the whole quantity
                    continue;
                }

                //2. assignment by preferrence
                var preferredBatchNumber = m_batchPreferrenceRepository.GetPrefferedBatchNumber(material.MaterialId);

                if (adHocPreferrence != null)
                {
                    preferredBatchNumber = adHocPreferrence.Item2.GetBatchNumber(m_batchRepository);
                }

                if (preferredBatchNumber != null)
                {
                    var preferredBatchesKey = new BatchKey(material.MaterialId, preferredBatchNumber);
                    var preferredBatches = m_batchRepository.GetBatches(preferredBatchesKey).ToList();

                    foreach (var preferredBatch in preferredBatches)
                    {
                        var availableBatchAmount = GetAvailableAmount(preferredBatch.Id);
                        if (availableBatchAmount.Value > 0m)
                        {
                            var allocateByPreferrence = m_amountProcessor.Min(amountToAllocate, availableBatchAmount);

                            if (adHocPreferrence != null)
                            {
                                allocateByPreferrence =
                                    m_amountProcessor.Min(
                                        new Amount(adHocPreferrence.Item3, material.Amount.Unit),
                                        allocateByPreferrence);
                            }

                            amountToAllocate = m_amountProcessor.Subtract(amountToAllocate, allocateByPreferrence);

                            result.Add(new OrderItemBatchAssignmentModel()
                            {
                                AssignedQuantity = allocateByPreferrence.Value,
                                MaterialBatchId = preferredBatch.Id,
                                BatchNumber = preferredBatch.BatchNumber,
                                OrderItemId = orderItem.Id
                            });

                            AssignOrderItemToBatch(preferredBatch.Id, order, orderItem.Id, allocateByPreferrence.Value);
                        }

                        if (availableBatchAmount.IsNotPositive)
                        {
                            m_batchPreferrenceRepository.InvalidatePreferrenceByMaterialId(material.MaterialId);
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
                    m_log.Error("Second attempt to AlignOrderBatches failed");
                }
                else
                {
                    return TryResolveBatchAssignmentsPrivate(m_orderRepository.GetOrder(order.Id), orderItemBatchPreference, true).ToList();
                }
            }

            return result;
        }

        public Tuple<decimal, BatchPrice> GetPriceOfAmount(IMaterialBatch batch, Amount amount, IBatchPriceBulkProvider provider)
        {
            var batchPrice = GetBatchPrice(batch, provider);

            var totalAmount = m_conversionHelper.ConvertAmount(new Amount(batchPrice.Batch.Volume, m_unitRepository.GetUnit(batchPrice.Batch.UnitId)), amount.Unit.Id);
            var pricePerUnit = batchPrice.TotalPriceInPrimaryCurrency / totalAmount.Value;

            return new Tuple<decimal, BatchPrice>(pricePerUnit * amount.Value, batchPrice);
        }

        public Amount GetNumberOfProducedProducts(int accountingDateYear, int accountingDateMonth, int inventoryId)
        {
            var allBatches = m_database.SelectFrom<IMaterialBatch>().Join(b => b.Material)
                .Where(b => b.ProjectId == m_session.Project.Id).Where(b => b.CloseDt == null)
                .Where(b => b.Material.InventoryId == inventoryId).Execute();

            return m_amountProcessor.Sum(allBatches.Where(b =>
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
            m_database.Sql()
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
            var order = m_orderRepository.GetOrder(orderId).Ensure();

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
                m_log.Error($"CutOrderAllocation - no allocations found orderId={orderId}, key={key?.ToString(this)}");
            }

            m_database.DeleteAll(assignmentsToCut);

            foreach (var cutBatchId in assignmentsToCut.Select(a => a.MaterialBatchId).Distinct())
            {
                ReleaseBatchAmountCache(cutBatchId);
            }
        }

        public IEnumerable<Tuple<int?, Amount>> ProposeAllocations(int materialId, string batchNumber, Amount requestedAmount)
        {
            var query = m_database.SelectFrom<IMaterialBatch>()
                .Where(b => b.CloseDt == null)
                .Where(b => b.ProjectId == m_session.Project.Id)
                .Where(b => b.MaterialId == materialId)
                .OrderBy(b => b.Created);

            if (string.IsNullOrWhiteSpace(batchNumber))
            {
                var material = m_materialRepository.GetMaterialById(materialId).Ensure();
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

                var allocatedFromThisBatch = m_amountProcessor.Min(available, requestedAmount);

                requestedAmount = m_amountProcessor.Subtract(requestedAmount, allocatedFromThisBatch);

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

            m_database.Sql().Call("GetMaterialResolution")
                .WithParam("@projectId", m_session.Project.Id)
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

                        batchIdNumberAmount.Add(new Tuple<int, string, Amount>(batchId, batchNumber, new Amount(available, m_unitRepository.GetUnit(unitId))));
                    });

            if (!string.IsNullOrWhiteSpace(batchNumberOrNull) && batchNumberIsPreferrence)
            {
                batchIdNumberAmount = batchIdNumberAmount.OrderByDescending(r =>
                    r.Item2.Equals(batchNumberOrNull, StringComparison.InvariantCultureIgnoreCase) ? 1 : 0).ToList();
            }

            var batchIdNumberTotalAllocated = new List<Tuple<int, string, Amount, Amount>>(batchIdNumberAmount.Count);

            foreach (var src in batchIdNumberAmount)
            {
                var canAllocateToThisBatch = m_amountProcessor.Min(demand, src.Item3);
                batchIdNumberTotalAllocated.Add(new Tuple<int, string, Amount, Amount>(src.Item1, src.Item2, src.Item3, canAllocateToThisBatch));

                demand = m_amountProcessor.Subtract(demand, canAllocateToThisBatch);
                if ((!includeBatchesWithoutAllocation) && (!demand.IsPositive))
                {
                    break;
                }
            }

            var result = new AllocationRequestResult(materialId, demand.IsNotPositive, m_amountProcessor.Sum(batchIdNumberTotalAllocated.Select(a => a.Item4)), demand);

            foreach (var batchNumber in batchIdNumberTotalAllocated.Select(b => b.Item2).Distinct())
            {
                var thisBatchNrAllocations = batchIdNumberTotalAllocated
                    .Where(a => a.Item2.Equals(batchNumber, StringComparison.InvariantCultureIgnoreCase)).ToList();

                batchCreated.TryGetValue(batchNumber.ToLowerInvariant(), out var dt);

                result.Allocations.Add(new BatchAllocation(
                    batchNumber, 
                    m_amountProcessor.Sum(thisBatchNrAllocations.Select(a => a.Item3)),
                    m_amountProcessor.Sum(thisBatchNrAllocations.Select(a => a.Item4)),
                    thisBatchNrAllocations.Select(a => new Tuple<int, Amount>(a.Item1, a.Item4)).ToList()
                    , dt));
            }

            return result;
        }

        public IEnumerable<PriceComponentModel> GetPriceComponents(int batchId, bool addSum = true)
        {
            var result = new List<PriceComponentModel>();

            m_database.Sql().Call("GetBatchPriceComponents")
                .WithParam("@batchId", batchId)
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@culture", m_session.Culture)
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
            var batches = m_batchRepository.GetBatches(key).Select(b => b.Id);

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
            return new BatchPriceBulkProvider(m_database, m_session, this, from, to);
        }

        private IList<BatchAmountProcedureResult> ExecBatchAmountProcedure(int? batchId, int? materialId)
        {
            var result = new List<BatchAmountProcedureResult>();

            m_database.Sql().Call("CalculateBatchUsages")
                .WithParam("@ProjectId", m_session.Project.Id)
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
            return m_batchRepository.GetBatchNumberAndMaterialIdByBatchId(batchId);
        }
    }
}
