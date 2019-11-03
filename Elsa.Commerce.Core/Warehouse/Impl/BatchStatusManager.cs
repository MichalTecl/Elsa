using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Model.BatchPriceExpl;
using Elsa.Commerce.Core.Repositories;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Utils;
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
        private readonly IFixedCostRepository m_fixedCostRepository;
        private readonly IUnitRepository m_unitRepository;

        private IOrdersFacade m_injectedOrdersFacade;
        private IMaterialBatchFacade m_injectedBatchFacade;

        private IOrdersFacade OrdersFacade => m_injectedOrdersFacade ?? (m_injectedOrdersFacade = m_serviceLocator.Get<IOrdersFacade>());

        private IMaterialBatchFacade BatchFacade =>
            m_injectedBatchFacade ?? (m_injectedBatchFacade = m_serviceLocator.Get<IMaterialBatchFacade>());

        public BatchStatusManager(IMaterialBatchRepository batchRepository, IPurchaseOrderRepository orderRepository, AmountProcessor amountProcessor, IDatabase database, IServiceLocator serviceLocator, IMaterialRepository materialRepository, IStockEventRepository eventRepository, IFixedCostRepository fixedCostRepository, IUnitRepository unitRepository)
        {
            m_batchRepository = batchRepository;
            m_orderRepository = orderRepository;
            m_amountProcessor = amountProcessor;
            m_database = database;
            m_serviceLocator = serviceLocator;
            m_materialRepository = materialRepository;
            m_eventRepository = eventRepository;
            m_fixedCostRepository = fixedCostRepository;
            m_unitRepository = unitRepository;
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

            model.BatchPrice = CalculateBatchPrice(batch.Batch.Id);

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
                () => m_eventRepository.GetBatchEvents(new BatchKey(batchId)).Where(e => e.BatchId == batchId));

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
        
        private BatchPrice CalculateBatchPrice(int batchId)
        {
            var batch = m_batchRepository.GetBatchById(batchId);
            if (batch == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            var result = new BatchPrice(batch.Batch);

            #region Purchase price
            if (batch.Batch.PriceConversion != null)
            {
                var conversion = batch.Batch.PriceConversion;
                result.AddComponent(false, null, $"Nákupní cena {StringUtil.FormatDecimal(conversion.TargetValue)}CZK <= ({StringUtil.FormatDecimal(conversion.SourceValue)}{conversion.SourceCurrency.Symbol} * {conversion.CurrencyRate.Rate})", conversion.TargetValue);
            }
            else if (batch.Batch.Price > 0)
            {
                result.AddComponent(false, null, $"Nákupní cena {StringUtil.FormatDecimal(batch.Batch.Price)} CZK", batch.Batch.Price);
            }
            #endregion

            #region Production work price
            if (batch.Batch.ProductionWorkPrice != null)
            {
                result.AddComponent(false,
                    null,
                    $"Cena práce při výrobě {batch.Batch.ProductionWorkPrice.Value.Display("CZK")}",
                    batch.Batch.ProductionWorkPrice.Value);
            }
            #endregion

            #region Price of components
            foreach (var component in batch.Components)
            {
                var componentUsedAmount = new Amount(component.ComponentAmount, component.ComponentUnit);
                var componentPrice = BatchFacade.GetPriceOfAmount(component.Batch.Id, componentUsedAmount);

                var priceEntry = result.AddComponent(false,
                    component.Batch.Id,
                    $"{StringUtil.FormatDecimal(componentUsedAmount.Value)}{componentUsedAmount.Unit.Symbol} {component.Batch.Material.Name} z šarže {component.Batch.BatchNumber} => {StringUtil.FormatDecimal(componentPrice.Item1)}CZK",
                    componentPrice.Item1);

                priceEntry.ChildPrices.Add(componentPrice.Item2);
            }
            #endregion

            #region Price of production steps
            foreach (var step in BatchFacade.GetProductionStepsProgress(batch.Batch))
            {
                string warn = null;

                if (m_amountProcessor.GreaterThan(step.RequiredAmount, step.TotalProducedAmount))
                {
                    warn = " POZOR - nejsou dokončeny všechny výrobní kroky, výpočet ceny není kompletní";
                }

                if (step.RequiredStep.PricePerUnit == null)
                {
                    warn = $" POZOR - krok {step.RequiredStep.Name}{warn}";
                }

                var stepPrice = (step.RequiredStep.PricePerUnit ?? 0) * step.TotalProducedAmount.Value;
                result.AddComponent(warn != null,
                    null,
                    $"Cena práce na {step.TotalProducedAmount} {step.RequiredStep.Name} => {stepPrice.Display("CZK")}{warn}",
                    stepPrice);


                var amountsOfBatchesUsedInStep = new Dictionary<int, Amount>();
                foreach (var performedStep in step.PerformedSteps)
                {
                    foreach (var stepSourceBatch in performedStep.SourceBatches)
                    {
                        amountsOfBatchesUsedInStep.TryGetValue(stepSourceBatch.SourceBatchId, out var sum);

                        var added = new Amount(stepSourceBatch.UsedAmount, stepSourceBatch.Unit ?? m_unitRepository.GetUnit(stepSourceBatch.UnitId));
                        if (sum != null)
                        {
                            added = m_amountProcessor.Add(added, sum);
                        }

                        amountsOfBatchesUsedInStep[stepSourceBatch.SourceBatchId] = added;
                    }
                }

                foreach (var stepSourceBatchIdAndAmount in amountsOfBatchesUsedInStep)
                {
                    var price = BatchFacade.GetPriceOfAmount(stepSourceBatchIdAndAmount.Key, stepSourceBatchIdAndAmount.Value);

                    var entry = result.AddComponent(false,
                        stepSourceBatchIdAndAmount.Key,
                        $"Pro {step.RequiredStep.Name} použito {stepSourceBatchIdAndAmount} {price.Item2.Batch.Material.Name} ze šarže {price.Item2.Batch.BatchNumber} => {price.Item1.Display("CZK")}",
                        price.Item1);

                    entry.ChildPrices.Add(price.Item2);
                }
            }
            #endregion

            #region Fixed costs
            var inventory = m_materialRepository.GetMaterialInventories()
                .FirstOrDefault(i => i.Id == batch.Batch.Material.InventoryId).Ensure();

            if (inventory.IncludesFixedCosts == true)
            {
                if (inventory.AllowedUnitId == null)
                {
                    throw new InvalidOperationException(
                        "Confiuration error - inventory with IncludesFixedCosts must have AllowedUnitId set");
                }

                var accountingDate = BatchFacade.GetBatchAccountingDate(batch.Batch);

                if (!accountingDate.IsFinal)
                {
                    result.AddComponent(true, null, $"Vypočtená cena není konečná: {accountingDate.NotFinalReason}", null);
                }

                var sum = 0m;
                var fixCostTypes = m_fixedCostRepository.GetFixedCostTypes();
                var allValues = m_fixedCostRepository.GetValues(accountingDate.AccountingDate.Year, accountingDate.AccountingDate.Month).ToList();
                
                var numOfAll = BatchFacade.GetNumberOfProducedProducts(accountingDate.AccountingDate.Year,
                    accountingDate.AccountingDate.Month,
                    inventory.Id);
                if (numOfAll.IsNotPositive)
                {
                    result.AddComponent(true,
                        null,
                        $"Nelze započítat fixní náklady, protože celková výroba v {accountingDate} má hodnotu {numOfAll}",
                        null);
                }
                else
                {
                    foreach (var costType in fixCostTypes)
                    {
                        var value = allValues.FirstOrDefault(v => v.FixedCostTypeId == costType.Id);
                        if (value == null)
                        {
                            result.AddComponent(true,
                                null,
                                $"Vypočtená cena není konečná - pro období {accountingDate} není zadána hodnota fixního nákladu \"{costType.Name}\"",
                                0);
                            continue;
                        }

                        var percentedPrice = value.Value / 100m * ((decimal)costType.PercentToDistributeAmongProducts);
                        var final = percentedPrice / numOfAll.Value * batch.Batch.Volume;

                        var text = $"Fixní náklad (({costType.Name} {accountingDate} = {value.Value.Display("CZK")}) * {costType.PercentToDistributeAmongProducts}% = {percentedPrice.Display("CZK")}) / (celkem výroba {accountingDate} = {numOfAll}) * {new Amount(batch.Batch)} = {final.Display("CZK")}";

                        result.AddComponent(false, null, text, final);
                    }
                }
            }
            #endregion

            return result;
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
            
            public BatchPrice BatchPrice { get; set; }
            
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
