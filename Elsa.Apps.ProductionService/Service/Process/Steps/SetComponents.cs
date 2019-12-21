using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.ProductionService.Models;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Commerce.Core.Warehouse.Impl.Model;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Recipes;

namespace Elsa.Apps.ProductionService.Service.Process.Steps
{
    internal class SetComponents : IProductionRequestProcessingStep
    {
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly AmountProcessor m_amountProcessor;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly IMaterialBatchRepository m_batchRepository;

        public SetComponents(IMaterialRepository materialRepository, IUnitRepository unitRepository, AmountProcessor amountProcessor, IMaterialBatchFacade batchFacade, IMaterialBatchRepository batchRepository)
        {
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_amountProcessor = amountProcessor;
            m_batchFacade = batchFacade;
            m_batchRepository = batchRepository;
        }

        public void Process(ProductionRequestContext context)
        {
            var recipeComponentList = context.Recipe.Components.OrderBy(c => c.SortOrder).ToList();

            for (var i = context.Request.Components.Count - 1; i >= 0; i--)
            {
                // kick out components not required by the recipe
                if (recipeComponentList.All(m => m.MaterialId != context.Request.Components[i].MaterialId))
                {
                    context.Request.Components.RemoveAt(i);
                }
            }

            var before = DateTime.Now;

            if (context.Request.SourceSegmentId != null)
            {
                var batch = m_batchRepository.GetBatchById(context.Request.SourceSegmentId.Value).Ensure();
                before = batch.Batch.Created;
            }
            
            foreach (var recipeComponent in recipeComponentList)
            {
                var requestComponent =
                    context.Request.Components.SingleOrDefault(c => c.MaterialId == recipeComponent.MaterialId);
                if (requestComponent == null)
                {
                    requestComponent = new ProductionComponent
                    {
                        IsValid = true,
                        MaterialId = recipeComponent.MaterialId,
                        MaterialName = m_materialRepository.GetMaterialById(recipeComponent.MaterialId).Name
                    };

                    context.Request.Components.Add(requestComponent);
                }

                requestComponent.RequiredAmount = recipeComponent.Amount * context.ComponentMultiplier;
                requestComponent.UnitSymbol = m_unitRepository.GetUnit(recipeComponent.UnitId).Symbol;
                requestComponent.SortOrder = recipeComponent.SortOrder;

                ProcessResolutions(context.Request, recipeComponent, requestComponent, context.ComponentMultiplier, before);
            }

            context.Request.Components.Sort(
                new Comparison<ProductionComponent>((a, b) => a.SortOrder.CompareTo(b.SortOrder)));
        }

        private void ProcessResolutions(ProductionRequest request, IRecipeComponent recipeComponent, ProductionComponent requestComponent, decimal multiplier, DateTime sourceBatchesMadeBefore)
        {
            if (TryProcessTransformationInput(request, recipeComponent, requestComponent) && (!requestComponent.IsValid))
            {
                return;
            }
            
            var requiredAmount = new Amount(recipeComponent.Amount * multiplier, m_unitRepository.GetUnit(recipeComponent.UnitId));

            ClearUserAllocationsIfQuantityChanged(requestComponent, requiredAmount);

            var requiredBatchNr = recipeComponent.IsTransformationInput ? request.ProducingBatchNumber : null;
            var resolutions = m_batchFacade.ResolveMaterialDemand(
                recipeComponent.MaterialId, 
                requiredAmount,
                requiredBatchNr,
                false, 
                true,
                sourceBatchesMadeBefore,
                request.SourceSegmentId);
            
            RemoveComponentsNotProposedBySystem(requestComponent, resolutions, requiredAmount);
            AddMissingComponents(requestComponent, resolutions, requiredAmount);

            var userAllocatedAmount =
                m_amountProcessor.Sum(requestComponent.Resolutions.Where(r => r.GetAmount(m_unitRepository) != null)
                    .Select(r => r.GetAmount(m_unitRepository))) ?? new Amount(0, requiredAmount.Unit);
            
            var remaining = m_amountProcessor.Subtract(requiredAmount, userAllocatedAmount);

            if (!resolutions.CompletelyAllocated)
            {
                requestComponent.Invalidate("Potřebné množství není dostupné");
            }

            if (remaining.IsPositive)
            {
                //seems we allocated the requested amount
                requestComponent.Invalidate($"Zbývá vložit {remaining}");
            }
            else if (remaining.IsNegative)
            {
                requestComponent.Invalidate($"Přebývá {m_amountProcessor.Neg(remaining)}");
            }
        }

        private static void ClearUserAllocationsIfQuantityChanged(ProductionComponent requestComponent, Amount requiredAmount)
        {
            if (!requiredAmount.ToString().Equals(requestComponent.LastClientAmount ?? string.Empty))
            {
                requestComponent.Resolutions.Clear();
                requestComponent.LastClientAmount = requiredAmount.ToString();
            }
        }

        private void AddMissingComponents(ProductionComponent requestComponent, AllocationRequestResult resolutions,
            Amount requiredAmount)
        {
            var clientComponentsCount = requestComponent.Resolutions.Count;
            foreach (var resolution in resolutions.Allocations)
            {
                var clientAllo = requestComponent.Resolutions.FirstOrDefault(a =>
                    a.BatchNumber.Equals(resolution.BatchNumber, StringComparison.InvariantCultureIgnoreCase));

                if (clientAllo == null)
                {
                    var convertedAllocatedAmount = m_amountProcessor.Convert(resolution.Allocated, requiredAmount.Unit);
                    var convertedAvailableAmount =
                        m_amountProcessor.Convert(resolution.TotalBatchNumberAvailable, requiredAmount.Unit);

                    requestComponent.Resolutions.Add(new ProductionComponentResolution
                    {
                        Amount = convertedAllocatedAmount.Value,
                        BatchAvailableAmount = convertedAvailableAmount.Value,
                        BatchAvailableAmountText = convertedAvailableAmount.ToString(),
                        UnitSymbol = convertedAllocatedAmount.Unit.Symbol,
                        BatchCreationDt = StringUtil.FormatDate(resolution.BatchCreated),
                        BatchNumber = resolution.BatchNumber,
                        Sorter = resolution.BatchCreated.Ticks,
                        Key = Guid.NewGuid().ToString()
                    });
                }
            }

            if ((clientComponentsCount > 0) && (requestComponent.Resolutions.Count > clientComponentsCount))
            {
                requestComponent.Resolutions.Sort((a, b) => a.Sorter.CompareTo(b.Sorter));
            }
        }

        private void RemoveComponentsNotProposedBySystem(ProductionComponent requestComponent,
            AllocationRequestResult resolutions, Amount requiredAmount)
        {
            for (var i = requestComponent.Resolutions.Count - 1; i >= 0; i--)
            {
                var clientResolution = requestComponent.Resolutions[i];

                var dbAllocation = resolutions.Allocations.FirstOrDefault(a =>
                    a.BatchNumber.Equals(clientResolution.BatchNumber, StringComparison.InvariantCultureIgnoreCase));

                if (dbAllocation == null)
                {
                    // we received allocation which was not proposed by the system
                    requestComponent.Resolutions.RemoveAt(i);
                    continue;
                }

                if ((clientResolution.GetAmount(m_unitRepository) == null) || m_amountProcessor.GreaterThan(
                        clientResolution.GetAmount(m_unitRepository),
                        dbAllocation.TotalBatchNumberAvailable))
                {
                    // this is an invalid allocation received from client
                    var convertedMaxAvailable =
                        m_amountProcessor.Convert(dbAllocation.TotalBatchNumberAvailable, requiredAmount.Unit);

                    clientResolution.Amount = convertedMaxAvailable.Value;
                    clientResolution.UnitSymbol = convertedMaxAvailable.Unit.Symbol;
                }
            }
        }

        private static bool TryProcessTransformationInput(ProductionRequest request, IRecipeComponent recipeComponent,
            ProductionComponent requestComponent)
        {
            if (recipeComponent.IsTransformationInput)
            {
                for (var i = requestComponent.Resolutions.Count - 1; i >= 0; i--)
                {
                    if (requestComponent.Resolutions[i].BatchNumber
                            ?.Equals(request.ProducingBatchNumber ?? string.Empty) != true)
                    {
                        requestComponent.Resolutions.RemoveAt(i);
                    }
                }

                if (string.IsNullOrWhiteSpace(request.ProducingBatchNumber))
                {
                    requestComponent.Invalidate("Receptura vyžaduje shodné číslo šarže");
                }

                return true;
            }

            return false;
        }
    }
}
