using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.ProductionService.Models;
using Elsa.Apps.ProductionService.Service.Process;
using Elsa.Apps.ProductionService.Service.Process.Steps;
using Elsa.Commerce.Core.Production.Recipes;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Recipes;
using Robowire;

namespace Elsa.Apps.ProductionService.Service
{
    public class ProductionService : IProductionService
    {
        private static readonly Type[] s_steps = new[]
        {
            typeof(LoadRecipe),
            typeof(SanitizeReceivedRequest),
            typeof(ApplyResultingMaterial),
            typeof(ValidateAmountAndPrice),
            typeof(SetComponents)
        };

        private readonly IServiceLocator m_serviceLocator;
        private readonly ISession m_session;

        public ProductionService(IServiceLocator serviceLocator, ISession session)
        {
            m_serviceLocator = serviceLocator;
            m_session = session;
        }


        public void ValidateRequest(ProductionRequest request)
        {
            var context = new ProductionRequestContext(m_session, request);

            foreach (var step in GetSteps())
            {
                step.Process(context);
            }

            request.IsValid = request.IsValid && request.Components.All(c => c.IsValid);
        }

        private IEnumerable<IProductionRequestProcessingStep> GetSteps()
        {
            foreach (var stepType in s_steps)
            {
                yield return (IProductionRequestProcessingStep)m_serviceLocator.InstantiateNow(stepType);
            }
        }
    }
}
