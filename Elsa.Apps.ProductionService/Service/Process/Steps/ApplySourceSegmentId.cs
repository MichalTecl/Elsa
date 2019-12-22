using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.ProductionService.Models;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Production.Recipes;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Apps.ProductionService.Service.Process.Steps
{
    internal class ApplySourceSegmentId : IProductionRequestProcessingStep
    {
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly AmountProcessor m_amountProcessor;
        private readonly IRecipeRepository m_recipeRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;

        public ApplySourceSegmentId(IMaterialBatchRepository batchRepository, 
            IMaterialBatchFacade batchFacade,
            AmountProcessor amountProcessor, 
            IRecipeRepository recipeRepository, IMaterialRepository materialRepository, IUnitRepository unitRepository)
        {
            m_batchRepository = batchRepository;
            m_batchFacade = batchFacade;
            m_amountProcessor = amountProcessor;
            m_recipeRepository = recipeRepository;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
        }

        public void Process(ProductionRequestContext context)
        {
            if (context.Request.SourceSegmentId == null)
            {
                return;
            }

            var request = context.Request;

            var sourceSegment = m_batchRepository.GetBatchById(context.Request.SourceSegmentId.Value).Ensure();

            request.OriginalBatchNumber = sourceSegment.Batch.BatchNumber;

            request.RecipeId = sourceSegment.Batch.RecipeId.Ensure("Šarže nevznikla z existující receptury");
            request.ProducingBatchNumber = request.ProducingBatchNumber ?? sourceSegment.Batch.BatchNumber;
            
            if (request.ProducingAmount == null)
            {
                request.ProducingAmount = sourceSegment.Batch.Volume;
            }
            
            request.ProducingPrice = sourceSegment.Batch.ProductionWorkPrice;

            var batchAmount = new Amount(sourceSegment.Batch.Volume, sourceSegment.Batch.Unit);
            var available = m_batchFacade.GetAvailableAmount(sourceSegment.Batch.Id);

            context.MinimalAmount = m_amountProcessor.Subtract(batchAmount, available);

            if (!context.Request.Components.Any())
            {
                ApplyComponents(sourceSegment, context);
            }
        }

        public void ApplyComponents(MaterialBatchComponent sourceSegment, ProductionRequestContext context)
        {
            var recipeComponents = m_recipeRepository.GetRecipe(sourceSegment.Batch.RecipeId.Ensure("Segment nevznikl z existující receptury, nelze změnit")).Components.OrderBy(c => c.SortOrder);

            var requestComponents = context.Request.Components;

            foreach (var recipeComponent in recipeComponents)
            {
                var compo = new ProductionComponent
                {
                    MaterialId = recipeComponent.MaterialId,
                    MaterialName = m_materialRepository.GetMaterialById(recipeComponent.MaterialId).Ensure().Name,
                    SortOrder = recipeComponent.SortOrder
                };
                requestComponents.Add(compo);

                var resolutions = sourceSegment.Components.Where(c => c.Batch.MaterialId == recipeComponent.MaterialId);

                var resIndex = new Dictionary<string, ProductionComponentResolution>();
                var componentUnit = m_unitRepository.GetUnit(recipeComponent.UnitId);

                foreach (var r in resolutions)
                {
                    var resolutionAmount = m_amountProcessor.Convert(new Amount(r.ComponentAmount, r.ComponentUnit), componentUnit);
                    var batchAvailability = m_amountProcessor.Convert(m_batchFacade.GetAvailableAmount(r.Batch.Id), componentUnit);

                    if (!resIndex.TryGetValue(r.Batch.BatchNumber, out var resolution))
                    {
                        resolution = new ProductionComponentResolution
                        {
                            Amount = resolutionAmount.Value,
                            BatchAvailableAmount = batchAvailability.Value,
                            BatchAvailableAmountText = batchAvailability.ToString(),
                            BatchCreationDt = StringUtil.FormatDate(r.Batch.Created),
                            BatchNumber = r.Batch.BatchNumber,
                            Key = Guid.NewGuid().ToString(),
                            Sorter = r.Batch.Created.Ticks,
                            UnitSymbol = componentUnit.Symbol
                        };
                        compo.Resolutions.Add(resolution);
                        resIndex.Add(r.Batch.BatchNumber, resolution);
                    }
                    else
                    {
                        resolution.Amount += resolutionAmount.Value;
                    }
                }

                compo.LastClientAmount = m_amountProcessor
                    .Sum(compo.Resolutions.Select(r => r.GetAmount(m_unitRepository)))?.ToString();
            }
        }
    }
}
