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

        public SetComponents(IMaterialRepository materialRepository, IUnitRepository unitRepository, AmountProcessor amountProcessor, IMaterialBatchFacade batchFacade)
        {
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_amountProcessor = amountProcessor;
            m_batchFacade = batchFacade;
        }

        public void Process(ProductionRequestContext context)
        {
            var recipeComponentList = context.Recipe.Components.OrderBy(c => c.SortOrder);

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

                ProcessResolutions(context.Request, recipeComponent, requestComponent, context.ComponentMultiplier);
            }
        }

        private void ProcessResolutions(ProductionRequest request, IRecipeComponent recipeComponent, ProductionComponent requestComponent, decimal multiplier)
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

                    return;
                }
            }

            var requiredAmount = new Amount(recipeComponent.Amount * multiplier, m_unitRepository.GetUnit(recipeComponent.UnitId));
            
            var requiredBatchNr = recipeComponent.IsTransformationInput ? request.ProducingBatchNumber : null;
            var resolutions = m_batchFacade.ResolveMaterialDemand(
                recipeComponent.MaterialId, 
                requiredAmount,
                requiredBatchNr,
                false, 
                true);
            
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
                
                if ((clientResolution.GetAmount(m_unitRepository) == null) || m_amountProcessor.GreaterThan(clientResolution.GetAmount(m_unitRepository),
                    dbAllocation.TotalBatchNumberAvailable))
                {
                    // this is an invalid allocation received from client
                    var convertedMaxAvailable =
                        m_amountProcessor.Convert(dbAllocation.TotalBatchNumberAvailable, requiredAmount.Unit);

                    clientResolution.Amount = convertedMaxAvailable.Value;
                    clientResolution.UnitSymbol = convertedMaxAvailable.Unit.Symbol;
                }
            }
            
            var userAllocatedAmount =
                m_amountProcessor.Sum(requestComponent.Resolutions.Where(r => r.GetAmount(m_unitRepository) != null)
                    .Select(r => r.GetAmount(m_unitRepository))) ?? new Amount(0, requiredAmount.Unit);

            var remaining = m_amountProcessor.Subtract(requiredAmount, userAllocatedAmount);

            if (remaining.IsZero && requestComponent.Resolutions.Any())
            {
                //seems we allocated the requested amount
                return;
            }

            //allocation is somehow invalid, let's propose new one

            requestComponent.Resolutions.Clear();

            foreach (var prop in resolutions.Allocations)
            {
                var convertedAllocatedAmount = m_amountProcessor.Convert(prop.Allocated, requiredAmount.Unit);
                var convertedAvailableAmount = m_amountProcessor.Convert(prop.TotalBatchNumberAvailable, requiredAmount.Unit);

                requestComponent.Resolutions.Add(new ProductionComponentResolution
                {
                    Amount = convertedAllocatedAmount.Value,
                    BatchAvailableAmount = convertedAvailableAmount.Value,
                    BatchAvailableAmountText = convertedAvailableAmount.ToString(),
                    UnitSymbol = convertedAllocatedAmount.Unit.Symbol,
                    BatchCreationDt = StringUtil.FormatDate(prop.BatchCreated),
                    BatchNumber = prop.BatchNumber
                });
            }

            if (!resolutions.CompletelyAllocated)
            {
                requestComponent.Invalidate("Potřebné množství není dostupné");
            }
        }
    }
}
