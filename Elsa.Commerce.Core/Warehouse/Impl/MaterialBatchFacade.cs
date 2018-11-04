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
using Elsa.Core.Entities.Commerce.Inventory.Batches;

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

        public void AssignOrderItemToBatch(int batchId, IPurchaseOrder order, IOrderItem orderItem, decimal assignmentQuantity)
        {
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

        public IEnumerable<OrderItemBatchAssignmentModel> CreateBatchesAssignmentProposal(IPurchaseOrder order)
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

            var result = new List<OrderItemBatchAssignmentModel>(items.Count * 2);

            var preferrences = m_batchPreferrenceRepository.GetPreferredBatches().ToList();
            
            foreach (var orderItem in items)
            {
                var material = m_virtualProductFacade.GetOrderItemMaterialForSingleUnit(order, orderItem);
                var preferrence = preferrences.FirstOrDefault(p => p.MaterialId == material.MaterialId);
                if (preferrence == null)
                {
                    result.Add(new OrderItemBatchAssignmentModel()
                                   {
                                       OrderItemId = orderItem.Id,
                                       AssignedQuantity = orderItem.Quantity,
                                       MaterialBatchId = -1
                                   });
                    continue;
                }

                var tookByPreferrence = Math.Min(orderItem.Quantity,  GetAvailableAmount(preferrence.BatchId).Value);
                
                result.Add(new OrderItemBatchAssignmentModel()
                {
                    OrderItemId = orderItem.Id,
                    AssignedQuantity = tookByPreferrence,
                    MaterialBatchId = preferrence.BatchId,
                    SourcePrefferenceId = preferrence.Id,
                    BatchNumber = m_batchRepository.GetBatchById(preferrence.BatchId)?.Batch.BatchNumber
                });

                if (tookByPreferrence < orderItem.Quantity)
                {
                    result.Add(new OrderItemBatchAssignmentModel()
                    {
                        OrderItemId = orderItem.Id,
                        AssignedQuantity = orderItem.Quantity - tookByPreferrence,
                        MaterialBatchId = -1
                    });
                }
            }

            return result;
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
    }
}
