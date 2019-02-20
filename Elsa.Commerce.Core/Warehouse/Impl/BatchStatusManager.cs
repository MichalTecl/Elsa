using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Impl
{
    public class BatchStatusManager : IBatchStatusManager
    {
        private static readonly TimeSpan s_cacheTimeout = TimeSpan.FromMinutes(0);

        private readonly IMaterialBatchRepository m_batchRepository;

        private readonly ICache m_cache = new CacheFake();
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly AmountProcessor m_amountProcessor;
        private readonly IDatabase m_database;
        private readonly IServiceLocator m_serviceLocator;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IStockEventRepository m_eventRepository;

        private IOrdersFacade m_injectedOrdersFacade;

        private IOrdersFacade OrdersFacade => m_injectedOrdersFacade ?? (m_injectedOrdersFacade = m_serviceLocator.Get<IOrdersFacade>());

        public BatchStatusManager(IMaterialBatchRepository batchRepository, IPurchaseOrderRepository orderRepository, AmountProcessor amountProcessor, IDatabase database, IServiceLocator serviceLocator, IMaterialRepository materialRepository, IStockEventRepository eventRepository)
        {
            m_batchRepository = batchRepository;
            m_orderRepository = orderRepository;
            m_amountProcessor = amountProcessor;
            m_database = database;
            m_serviceLocator = serviceLocator;
            m_materialRepository = materialRepository;
            m_eventRepository = eventRepository;
        }

        public IMaterialBatchStatus GetStatus(int batchId)
        {
           return GetStatusWithoutStep(batchId, -1);
        }

        public IMaterialBatchStatus GetStatusWithoutStep(int batchId, int filteredProductionStepId)
        {
            var batch = m_batchRepository.GetBatchById(batchId);

            var model = new BatchStatus(batch.Batch);

            PopulateBatchInfo(batchId, model);
            PopulateOwnSteps(batchId, model);
            PopulateForeignSteps(batchId, model);
            PopulateEvents(batchId, model);
            PopulateOrders(batchId, model);
            PopulateCompositions(batchId, model);

            var mainBatchAmount = model.CalculateAvailableAmount(m_amountProcessor, filteredProductionStepId);

            model.CurrentAvailableAmount = mainBatchAmount;

            return model;
        }
        
        private void PopulateForeignSteps(int batchId, BatchStatus model)
        {
            var steps = m_cache.ReadThrough(GetForeignStepsCacheKey(batchId), s_cacheTimeout,
                () =>
                    {
                        return
                            m_database.SelectFrom<IBatchProuctionStepSourceBatch>()
                                .Join(s => s.Unit)
                                .Where(s => s.SourceBatchId == batchId)
                                .Execute();
                    });

            model.UsedInSteps.AddRange(steps);
        }
        
        private void PopulateOwnSteps(int batchId, BatchStatus model)
        {
            var steps = m_cache.ReadThrough(GetOwnStepsCacheKey(batchId), s_cacheTimeout,
                () =>
                {
                    return
                        m_database.SelectFrom<IBatchProductionStep>()
                            .Where(s => s.BatchId == batchId)
                            .Execute();
                });

            model.ResolvedSteps.AddRange(steps);
        }
       
 
        private void PopulateCompositions(int batchId, BatchStatus model)
        {
            var compositions = m_cache.ReadThrough(GetCompositionsCacheKey(batchId), s_cacheTimeout,
                () => m_batchRepository.GetCompositionsByComponentBatchId(batchId));

            model.UsedInCompositions.AddRange(compositions);
        }
        
        private void PopulateOrders(int batchId, BatchStatus model)
        {
            var assignments = m_cache.ReadThrough(
                GetOrdersCacheKey(batchId),
                s_cacheTimeout,
                () =>
                    {

                        var result = new List<IOrderItemMaterialBatch>();

                        var orders = m_orderRepository.GetOrdersByMaterialBatch(batchId);

                        foreach (var order in orders)
                        {
                            if (OrderStatus.IsUnsuccessfullyClosed(order.OrderStatusId))
                            {
                                continue;
                            }

                            foreach (var item in OrdersFacade.GetAllConcreteOrderItems(order))
                            {
                                foreach (var assignment in
                                    item.AssignedBatches.Where(a => a.MaterialBatchId == batchId))
                                {
                                    result.Add(assignment);
                                }
                            }
                        }

                        return result;
                    });

            model.UsedInOrderItems.AddRange(assignments);
        }

        private void PopulateEvents(int batchId, BatchStatus model)
        {
            var events = m_cache.ReadThrough(GetEventsCacheKey(batchId), s_cacheTimeout,
                () => m_eventRepository.GetBatchEvents(batchId));

            model.Events.AddRange(events);
        }
        
        private void PopulateBatchInfo(int batchId, BatchStatus model)
        {
            var batch = m_batchRepository.GetBatchById(batchId);
            if (batch == null)
            {
                throw new InvalidOperationException("Invalid batch reference");
            }

            var materialSteps = m_materialRepository.GetMaterialProductionSteps(batch.Batch.MaterialId);

            model.RequiredSteps.AddRange(materialSteps);
        }

        #region Invalidations
        public void OnBatchChanged(int batchId)
        {
        }

        public void NotifyProductionStepChanged(int batchId)
        {
        }

        public void NotifyBatchUsageInStep(int batchId)
        {
        }

        public void NotifyBatchEvent(int batchId)
        {
        }

        public void NotifyComposition(int batchId)
        {
        }

        public void NotifyBatchOrderUsage(int batchId)
        {
        }

        #endregion

        #region Cache Keys
        
        private static string GetOrdersCacheKey(int batchId)
        {
            return $"btst_orders_{batchId}";
        }

        private static string GetBatchCacheKey(int batchId)
        {
            return $"btst_calculation_{batchId}";
        }

        private static string GetForeignStepsCacheKey(int batchId)
        {
            return $"btst_frnsts_{batchId}";
        }
        private static string GetEventsCacheKey(int batchId)
        {
            return $"btst_evts_{batchId}";
        }
        private static string GetOwnStepsCacheKey(int batchId)
        {
            return $"btst_ownsts_{batchId}";
        }

        private string GetCompositionsCacheKey(int batchId)
        {
            return $"btst_cmpsts_{batchId}";
        }

        #endregion

        #region Nested
        private sealed class BatchStatus : IMaterialBatchStatus
        {
            private readonly IMaterialBatch m_batch;

            public BatchStatus(IMaterialBatch batch)
            {
                this.m_batch = batch;
            }

            public List<IMaterialProductionStep> RequiredSteps { get; } = new List<IMaterialProductionStep>();

            public List<IBatchProductionStep> ResolvedSteps { get; } = new List<IBatchProductionStep>();
            
            public List<IMaterialStockEvent> Events { get; } = new List<IMaterialStockEvent>();
            
            public List<IOrderItemMaterialBatch> UsedInOrderItems { get; } = new List<IOrderItemMaterialBatch>();
            
            public List<IMaterialBatchComposition> UsedInCompositions { get; } = new List<IMaterialBatchComposition>();

            public List<IBatchProuctionStepSourceBatch> UsedInSteps { get; } = new List<IBatchProuctionStepSourceBatch>();

            public Amount CurrentAvailableAmount { get; set; }

            public Amount CalculateAvailableAmount(AmountProcessor amountProcessor, int filteredStepId)
            {
                var steps = new List<Func<Amount, Amount>>();

                steps.Add(a => m_batch.IsAvailable && (m_batch.CloseDt == null) ? new Amount(m_batch.Volume, m_batch.Unit) : null);

                // Resolved production steps
                steps.Add(
                    mainBatchAmount =>
                    {
                        foreach (var requiredStep in RequiredSteps.Ordered().Reverse())
                        {
                            var resolvedSteps = ResolvedSteps.Where(s => (s.Id != filteredStepId) && (s.StepId == requiredStep.Id)).Distinct().ToList();
                            if (resolvedSteps.Count == 0)
                            {
                                return new Amount(0, mainBatchAmount.Unit);
                            }

                            var resolvedStepsSum =
                            amountProcessor.Sum(
                                resolvedSteps.Select(s => new Amount(s.ProducedAmount, m_batch.Unit)));

                            mainBatchAmount = amountProcessor.Min(mainBatchAmount, resolvedStepsSum);
                        }

                        return mainBatchAmount;
                    });

                //Events
                steps.Add(mainBatchAmount =>
                {
                    var eventsSum = amountProcessor.Sum(Events.Select(e => new Amount(e.Type.IsSubtracting ? -1m * e.Delta : e.Delta, e.Unit)));
                    return amountProcessor.Add(mainBatchAmount, eventsSum);
                });

                //Orders
                steps.Add(mainBatchAmount =>
                {
                    var eventsSum = amountProcessor.Sum(UsedInOrderItems.Select(o => new Amount(o.Quantity, m_batch.Unit)));
                    return amountProcessor.Subtract(mainBatchAmount, eventsSum);
                });

                //Compositions
                steps.Add(mainBatchAmount =>
                {
                    var compoSum = amountProcessor.Sum(UsedInCompositions.Select(c => new Amount(c.Volume, c.Unit)));
                    return amountProcessor.Subtract(mainBatchAmount, compoSum);
                });

                //Used in step of another batch
                steps.Add(
                    mainBatchAmount =>
                    {
                        var stpSum = amountProcessor.Sum(UsedInSteps.Select(s => new Amount(s.UsedAmount, s.Unit)));
                        return amountProcessor.Subtract(mainBatchAmount, stpSum);
                    });

                Amount amount = null;
                foreach (var step in steps)
                {
                    amount = step(amount);

                    if (amount == null)
                    {
                        return new Amount(0, m_batch.Unit);
                    }
                }

                return amount;
            }
        }

        #endregion
    }
}
