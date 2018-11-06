using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Impl
{
    public class MaterialBatchFacade : IMaterialBatchFacade
    {
        private static readonly object s_amountLock = new object();
        private readonly ILog m_log;
        private readonly IVirtualProductFacade m_virtualProductFacade;
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly AmountProcessor m_amountProcessor;
        private readonly ICache m_cache;
        private readonly IDatabase m_database;
        private readonly IPackingPreferredBatchRepository m_batchPreferrenceRepository;
        private readonly IKitProductRepository m_kitProductRepository;

        public MaterialBatchFacade(
            ILog log,
            IVirtualProductFacade virtualProductFacade,
            IMaterialBatchRepository batchRepository,
            IPurchaseOrderRepository orderRepository,
            AmountProcessor amountProcessor,
            ICache cache,
            IDatabase database, 
            IPackingPreferredBatchRepository batchPreferrenceRepository, IKitProductRepository kitProductRepository)
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
            }
        }
        
        public Amount GetAvailableAmount(int batchId)
        {
            return m_cache.ReadThrough(
                GetBatchAmountCacheKey(batchId),
                TimeSpan.FromMinutes(10),
                () =>
                    {

                        var batch = m_batchRepository.GetBatchById(batchId);
                        if (batch == null)
                        {
                            throw new InvalidOperationException("Invalid batch reference");
                        }

                        return GetAvailableAmount(batch);
                    });
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
                throw new InvalidOperationException("Více než jedna šarže odpovídá zadanému číslu, použijte prosím celé číslo šarže");
            }

            throw new InvalidOperationException("Šarže nebyla nalezena");
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
                    if (requestNewAmount == null || requestNewAmount == 0m)
                    {
                        m_database.Delete(oldAssignment);
                        tx.Commit();
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

        private Amount GetAvailableAmount(MaterialBatchComponent batch)
        {
            return m_cache.ReadThrough(
                GetBatchAmountCacheKey(batch.Batch.Id),
                TimeSpan.FromMinutes(10),
                () =>
                    {
                        if (batch.IsClosed || batch.IsLocked)
                        {
                            return new Amount(decimal.Zero, batch.ComponentUnit);
                        }

                        var batchAmount = new Amount(batch.ComponentAmount, batch.ComponentUnit);

                        var orders = m_orderRepository.GetOrdersByMaterialBatch(batch.Batch.Id);

                        foreach (var order in orders)
                        {
                            if (OrderStatus.IsUnsuccessfullyClosed(order.OrderStatusId))
                            {
                                continue;
                            }
                            
                            foreach (var item in order.Items)
                            {
                                foreach (var assignment in item.AssignedBatches.Where(a => a.MaterialBatchId == batch.Batch.Id))
                                {
                                    var itemAmount = new Amount(assignment.Quantity, batch.ComponentUnit);
                                    batchAmount = m_amountProcessor.Subtract(batchAmount, itemAmount);
                                }
                            }

                            foreach (var evt in m_batchRepository.GetBatchEvents(batch.Batch.Id))
                            {
                                batchAmount = m_amountProcessor.Add(batchAmount, new Amount(evt.Delta, evt.Unit));
                            }
                        }

                        return batchAmount;
                    });
        }

        private string GetBatchAmountCacheKey(int batchId)
        {
            return $"btchamnt_{batchId}";
        }

        private void InvalidateBatchCache(int batchId)
        {
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

                foreach (var kitItem in kitItems)
                {
                    if (kitItem.SelectedItem != null)
                    {
                        items.Add(kitItem.SelectedItem);
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

                if (preferrence != null || adHocPreferrence != null)
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

                    if (availableBatchAmount.IsNotPositive && preferrence != null)
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
    }
}
