using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Utils;

namespace Elsa.Apps.ProductionService.Service.Process.Steps
{
    internal class ApplyResultingMaterial : IProductionRequestProcessingStep
    {
        private readonly IMaterialRepository m_materialRepository;
        private readonly IMaterialFacade m_materialFacade;
        private readonly IUnitRepository m_unitRepository;
        private readonly IUnitConversionHelper m_conversionHelper;

        public ApplyResultingMaterial(IMaterialRepository materialRepository, IMaterialFacade materialFacade,
            IUnitRepository unitRepository, IUnitConversionHelper conversionHelper)
        {
            m_materialRepository = materialRepository;
            m_materialFacade = materialFacade;
            m_unitRepository = unitRepository;
            m_conversionHelper = conversionHelper;
        }

        public void Process(ProductionRequestContext context)
        {
            var material = m_materialFacade.GetMaterialInfo(context.Recipe.ProducedMaterialId);

            context.Request.MaterialName = material.MaterialName;

            // Batch number
            if (string.IsNullOrWhiteSpace(context.Request.ProducingBatchNumber))
            {
                if (material.AutomaticBatches)
                {
                    context.Request.ProducingBatchNumber = material.AutoBatchNr;
                }
                else
                {
                    context.InvalidateRequest("Nutno vyplnit číslo šarže");
                }
            }

            // Unit
            while (true)
            {
                if (string.IsNullOrWhiteSpace(context.Request.ProducingUnitSymbol))
                {
                    context.Request.ProducingUnitSymbol = material.PreferredUnitSymbol;
                    break;
                }

                if (!context.Request.ProducingUnitSymbol.Equals(material.PreferredUnitSymbol, StringComparison.InvariantCultureIgnoreCase))
                {
                    var usedUnit = m_unitRepository.GetUnitBySymbol(context.Request.ProducingUnitSymbol.Trim());
                    if (usedUnit == null)
                    {
                        context.Request.ProducingUnitSymbol = null;
                        continue;
                    }
                    
                    var mainUnit = m_unitRepository.GetUnitBySymbol(material.PreferredUnitSymbol);
                    if (!m_conversionHelper.AreCompatible(usedUnit.Id, mainUnit.Id))
                    {
                        context.Request.ProducingUnitSymbol = null;
                        continue;
                    }
                }

                break;
            }

            context.RequestedAmount = new Amount(context.Request.ProducingAmount ?? 0m, m_unitRepository.GetUnitBySymbol(context.Request.ProducingUnitSymbol));
            context.NominalRecipeAmount = new Amount(context.Recipe.RecipeProducedAmount, m_unitRepository.GetUnit(context.Recipe.ProducedAmountUnitId));

            var commonUnit = m_conversionHelper.GetSmallestCompatibleUnit(context.RequestedAmount.Unit);

            var convertedRequestedAmount = m_conversionHelper.ConvertAmount(context.RequestedAmount, commonUnit.Id);
            var convertedNominalAmount = m_conversionHelper.ConvertAmount(context.NominalRecipeAmount, commonUnit.Id);

            context.ComponentMultiplier = convertedRequestedAmount.Value / convertedNominalAmount.Value;
        }
    }
}
