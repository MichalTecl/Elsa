using System.Linq;

namespace Elsa.Apps.ProductionService.Service.Process.Steps
{
    internal class AddBatchChangeWarning : IProductionRequestProcessingStep
    {
        public void Process(ProductionRequestContext context)
        {
            foreach (var component in context.Request.Components)
            {
                component.HasBatchChangeWarning =
                    (component.IsValid && component.Resolutions.Count(r => r.Amount > 0) > 1);
            }
        }
    }
}
