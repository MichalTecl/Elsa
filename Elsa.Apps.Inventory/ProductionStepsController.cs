using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Production;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.Warehouse;
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

        public ProductionStepsController(IWebSession webSession, ILog log, IProductionFacade productionFacade, AmountProcessor amountProcessor) : base(webSession, log)
        {
            m_productionFacade = productionFacade;
            m_amountProcessor = amountProcessor;
        }

        public IEnumerable<MaterialBatchViewModel> FindBatchesToProceed(string query)
        {
            return MaterialBatchViewModel.JoinAutomaticBatches(
                m_productionFacade.FindBatchesWithUnresolvedProductionSteps(query)
                    .Select(b => new MaterialBatchViewModel(b)).OrderBy(mb => mb.MaterialName), m_amountProcessor);
        }

        public IEnumerable<ProductionStepViewModel> GetStepsToProceed(int materialId, int? materialBatchId)
        {
            return m_productionFacade.GetStepsToProceed(materialBatchId, materialId);
        }

        public ProductionStepViewModel Validate(ProductionStepViewModel model)
        {
            return m_productionFacade.UpdateProductionStep(model);
        }

        public IEnumerable<ProductionStepViewModel> SaveProductionStep(ProductionStepViewModel model)
        {
            throw new NotImplementedException();
        }


    }
}
