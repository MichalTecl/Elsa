using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Model.BatchPriceExpl;
using Elsa.Commerce.Core.Model.ProductionSteps;
using Elsa.Commerce.Core.Repositories;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Commerce.Core.Warehouse.Thresholds;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Impl
{
    public class MaterialBatchFacade : IMaterialBatchFacade
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
        private readonly IBatchStatusManager m_batchStatusManager;
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
            IBatchStatusManager batchStatusManager,
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
            m_batchStatusManager = batchStatusManager;
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

            InvalidateBatchCache(batchId);

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

                InvalidateBatchCache(batchId);
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

        public IEnumerable<OrderItemBatchAssignmentModel> TryResolveBatchAssignments(IPurchaseOrder order, Tuple<long, int, decimal> orderItemBatchPreference = null)
        {
            return TryResolveBatchAssignmentsPrivate(order, orderItemBatchPreference, false).ToList();
        }

        public IMaterialBatch FindBatchBySearchQuery(int materialId, string query)
        {
            var batches = m_batchRepository.GetMaterialBatches(
                DateTime.Now.AddDays(-365),
                DateTime.Now.AddDays(365),
                false,
                materialId);

            var found = batches.Where(b => b.Batch.BatchNumber.EndsWith(query)).ToList();

            if (found.Count == 1)
            {
                return found[0].Batch;
            }

            if (found.Count > 1)
            {
                throw new InvalidOperationException($"Více než jedna šarže odpovídá zadanému číslu \"{query}\" , použijte prosím celé číslo šarže");
            }

            throw new InvalidOperationException($"Šarže \"{query}\" nebyla nalezena");
        }

        public bool AlignOrderBatches(long purchaseOrderId)
        {
            var result = false;

            var preferrences = new Dictionary<int, Tuple<int, DateTime>>();

            using (var tx = m_database.OpenTransaction())
            {
                var order = m_orderRepository.GetOrder(purchaseOrderId);
                var allItems = GetAllOrderItems(order);

                foreach (var item in allItems)
                {
                    var assignments = item.AssignedBatches.ToList();
                    if (assignments.Count < 2)
                    {
                        if (assignments.Count == 1)
                        {
                            Tuple<int, DateTime> preference;
                            if ((!preferrences.TryGetValue(assignments[0].MaterialBatch.MaterialId, out preference)) || (preference.Item2 < assignments[0].AssignmentDt))
                            {
                                preferrences[assignments[0].MaterialBatch.MaterialId] = new Tuple<int, DateTime>(assignments[0].MaterialBatchId, assignments[0].AssignmentDt);
                            }
                        }
                        continue;
                    }

                    result = AlignAssignments(assignments) | result;
                }

                foreach (var pref in preferrences.Values)
                {
                    if (GetAvailableAmount(pref.Item1).IsPositive)
                    {
                        m_batchPreferrenceRepository.SetBatchPreferrence(pref.Item1);
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

        public void ChangeOrderItemBatchAssignment(
            IPurchaseOrder order,
            long orderItemId,
            int batchId,
            decimal? requestNewAmount)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var item = GetAllOrderItems(order).FirstOrDefault(i => i.Id == orderItemId);
                if (item == null)
                {
                    throw new InvalidOperationException("Invalid item id");
                }

                var oldAssignment = item.AssignedBatches.FirstOrDefault(b => b.MaterialBatchId == batchId);
                if (oldAssignment == null)
                {
                    throw new InvalidOperationException("Invalid batch id");
                }
                
                m_batchPreferrenceRepository.RemoveBatchFromPreferrence(batchId);

                try
                {
                    if ((requestNewAmount == null) || (requestNewAmount == 0m))
                    {
                        m_database.Delete(oldAssignment);
                        tx.Commit();
                        InvalidateBatchCache(oldAssignment.MaterialBatchId);
                        return;
                    }

                    if (requestNewAmount.Value > oldAssignment.Quantity)
                    {
                        m_database.Delete(oldAssignment);

                        InvalidateBatchCache(batchId);
                        TryResolveBatchAssignments(
                            order,
                            new Tuple<long, int, decimal>(orderItemId, batchId, requestNewAmount.Value));

                        tx.Commit();
                        return;
                    }

                    oldAssignment.Quantity = requestNewAmount.Value;
                    m_database.Save(oldAssignment);

                    tx.Commit();
                }
                finally
                {
                    InvalidateBatchCache(batchId);
                }
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
                InvalidateBatchCache(parentBatchId);
                InvalidateBatchCache(componentBatchId);

                m_batchStatusManager.OnBatchChanged(parentBatchId);
                m_batchStatusManager.OnBatchChanged(componentBatchId);
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
                InvalidateBatchCache(parentBatchId);
                InvalidateBatchCache(componentBatchId);
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
                    InvalidateBatchCache(compo.ComponentId);
                }

                InvalidateBatchCache(batchId);

                tx.Commit();
            }
        }

        public void ReleaseBatchAmountCache(IMaterialBatch batch)
        {
            InvalidateBatchCache(batch.Id);
        }

        public IEnumerable<string> GetDeletionBlockReasons(int batchId)
        {
            var batch = m_batchRepository.GetBatchById(batchId);
            if (batch == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            var status = GetBatchStatus(batchId);

            if (status.UsedInOrderItems.Any())
            {
                yield return "Již bylo prodáno zboží z této šarže";
            }

            if (status.UsedInCompositions.Any() || status.UsedInSteps.Any())
            {
                yield return "Šarže je použita ve složení jiné šarže";
            }

            foreach (var evt in status.Events.Select(e => e.Type.Name).Distinct())
            {
                yield return $"Byla provedena akce typu \"{evt}\"";
            }

            if (status.ResolvedSteps.Any())
            {
                yield return "Byly proveden výrobní kroky";
            }
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

            var status = GetBatchStatus(batch.Batch.Id);

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

        public IEnumerable<IMaterialBatch> FindNotClosedBatches(int inventoryId, DateTime @from, DateTime to, Func<IMaterialBatch, bool> filter = null)
        {
            return m_batchRepository
                .QueryBatches(q =>
                    q.Join(b => b.Material).Where(b => b.Material.InventoryId == inventoryId)
                        .Where(b => (b.Created >= @from) && (b.Created <= to)).Where(b => b.CloseDt == null)).Where(b => filter?.Invoke(b) != false)
                .Select(b => m_batchRepository.GetBatchById(b.Id).Batch);
        }

        public IEnumerable<BatchStepProgressInfo> GetProductionStepsProgress(IMaterialBatch batch)
        {
            var material = m_materialRepository.GetMaterialById(batch.MaterialId);

            var requiredAmount = m_conversionHelper.ConvertAmount(new Amount(batch.Volume, batch.Unit ?? m_unitRepository.GetUnit(batch.UnitId)), material.NominalUnit.Id);
            
            var allPerformedSteps = m_batchRepository.GetPerformedSteps(batch.Id).ToList();

            foreach (var materialProductionStep in m_materialRepository.GetMaterialProductionSteps(batch.MaterialId).Ordered())
            {
                var performed = allPerformedSteps.Where(s => s.StepId == materialProductionStep.Id).ToList();
                var totalProduced = new Amount(performed.Sum(s => s.ProducedAmount), requiredAmount.Unit);

                yield return new BatchStepProgressInfo(materialProductionStep, requiredAmount, totalProduced, performed);
            }
        }

        public BatchAccountingDate GetBatchAccountingDate(IMaterialBatch batch)
        {
            if (batch.FinalAccountingDate != null)
            {
                return new BatchAccountingDate(batch.FinalAccountingDate.Value);
            }

            var d = batch.Created;
            var sb = new StringBuilder();

            var batchAmount = new Amount(batch.Volume, m_unitRepository.GetUnit(batch.UnitId));

            var events = m_stockEventRepository.GetBatchEvents(batch.Id);
            foreach(var evt in events)
            {
                batchAmount = m_amountProcessor.Subtract(batchAmount,
                    new Amount(evt.Delta, evt.Unit ?? m_unitRepository.GetUnit(evt.UnitId)));
            }
            
            foreach (var step in GetProductionStepsProgress(batch))
            {
                var maxDt = step.PerformedSteps.Any() ? step.PerformedSteps.Max(s => s.ConfirmDt) : d;
                if (maxDt > d)
                {
                    d = maxDt;
                }

                var reminingAmount = m_amountProcessor.Subtract(step.RequiredAmount, step.TotalProducedAmount);
                if (reminingAmount.IsPositive)
                {
                    sb.AppendLine($"Zbývá {reminingAmount} {step.RequiredStep.Name}");
                }
            }

            if (sb.Length == 0)
            {
                m_batchRepository.UpdateBatch(batch.Id, b => b.FinalAccountingDate = d);
                return new BatchAccountingDate(d);
            }

            return new BatchAccountingDate(d, false, sb.ToString());
        }
        
        public IMaterialBatchStatus GetBatchStatus(int batchId)
        {
            return m_cache.ReadThrough(GetBatchStatusCacheKey(batchId),
                TimeSpan.FromMinutes(10),
                () => m_batchStatusManager.GetStatus(batchId));
        }

        public BatchPrice GetBatchPrice(int batchId)
        {
            return GetBatchStatus(batchId).Ensure().BatchPrice;
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

        private void InvalidateBatchCache(int batchId)
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

        private IEnumerable<OrderItemBatchAssignmentModel> TryResolveBatchAssignmentsPrivate(IPurchaseOrder order, Tuple<long, int, decimal> orderItemBatchPreference, bool doNotRetry)
        {
            var items = GetAllOrderItems(order);

            var result = new List<OrderItemBatchAssignmentModel>(items.Count * 2);

            var preferrences = m_batchPreferrenceRepository.GetPreferredBatches().ToList();

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
                var preferrence = preferrences.FirstOrDefault(p => p.MaterialId == material.MaterialId);

                if ((preferrence != null) || (adHocPreferrence != null))
                {
                    var preferredBatch = adHocPreferrence?.Item2 ?? preferrence.BatchId;

                    var availableBatchAmount = GetAvailableAmount(preferredBatch);
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
                            MaterialBatchId = preferredBatch,
                            BatchNumber = m_batchRepository.GetBatchById(preferredBatch)?.Batch?.BatchNumber,
                            OrderItemId = orderItem.Id
                        });

                        AssignOrderItemToBatch(preferredBatch, order, orderItem.Id, allocateByPreferrence.Value);
                    }

                    if (availableBatchAmount.IsNotPositive && (preferrence != null))
                    {
                        preferrences.Remove(preferrence);
                        m_batchPreferrenceRepository.InvalidatePreferrence(preferrence.Id);
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

        public Tuple<decimal, BatchPrice> GetPriceOfAmount(int batchId, Amount amount)
        {
            var batchPrice = GetBatchPrice(batchId);

            var totalAmount = m_conversionHelper.ConvertAmount(new Amount(batchPrice.Batch), amount.Unit.Id);
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
                InvalidateBatchCache(batchId);
            }
        }

        /// <summary>
        /// Use key = null to delete all allocations from this order
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="key"></param>
        public void CutOrderAllocation(int orderId, BatchKey key)
        {
            var order = m_orderRepository.GetOrder(orderId).Ensure();

            if (OrderStatus.IsSent(order.OrderStatusId))
            {
                throw new InvalidOperationException($"Tato objednávka již byla odeslána");
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
                InvalidateBatchCache(cutBatchId);
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
                    throw new InvalidOperationException($"Chybi cislo sarze");
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
