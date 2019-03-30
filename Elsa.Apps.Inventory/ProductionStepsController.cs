using System.Collections.Generic;
using System.Linq;

using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Production;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("productionSteps")]
    public class ProductionStepsController : ElsaControllerBase
    {
        private readonly IProductionFacade m_productionFacade;
        private readonly AmountProcessor m_amountProcessor;
        private readonly IMaterialRepository m_materialRepository;
        private readonly ISupplierRepository m_supplierRepository;

        public ProductionStepsController(IWebSession webSession,
            ILog log,
            IProductionFacade productionFacade,
            AmountProcessor amountProcessor,
            IMaterialRepository materialRepository,
            ISupplierRepository supplierRepository)
            : base(webSession, log)
        {
            m_productionFacade = productionFacade;
            m_amountProcessor = amountProcessor;
            m_materialRepository = materialRepository;
            m_supplierRepository = supplierRepository;
        }

        public IEnumerable<MaterialWithStepsViewModel> GetAllMaterialsWithSteps()
        {
            var allSteps = m_materialRepository.GetMaterialProductionSteps().OrderBy(s => s.MaterialId).ThenBy(s => s.Name);

            var result = new List<MaterialWithStepsViewModel>();

            foreach (var step in allSteps)
            {
                var material = result.FirstOrDefault(m => m.MaterialId == step.MaterialId);
                if (material == null)
                {
                    material = new MaterialWithStepsViewModel()
                    {
                        IsAutoBatch = step.Material.AutomaticBatches,
                        MaterialId = step.MaterialId,
                        MaterialName = step.Material.Name
                    };
                    
                    result.Add(material);
                }

                material.AddStep(step.Id, step.Name);
            }

            return result;
        }

        public IEnumerable<MaterialBatchViewModel> FindBatchesToProceed(string query)
        {
            return MaterialBatchViewModel.JoinAutomaticBatches(
                m_productionFacade.FindBatchesWithUnresolvedProductionSteps(query)
                    .Select(b => new MaterialBatchViewModel(b, m_supplierRepository)).OrderBy(mb => mb.MaterialName),
                m_amountProcessor);
        }

        public IEnumerable<ProductionStepViewModel> GetStepsToProceed(int materialId, int? materialBatchId)
        {
            return m_productionFacade.GetStepsToProceed(materialBatchId, materialId);
        }

        public ProductionStepViewModel Validate(ProductionStepViewModel model)
        {
            return m_productionFacade.UpdateProductionStep(model);
        }

        public void SaveProductionStep(ProductionStepViewModel model)
        {
            m_productionFacade.SaveProductionStep(model);
        }


    }
}
