using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.ProductionService.Service.Process.Steps
{
    internal class SanitizeReceivedRequest : IProductionRequestProcessingStep
    {
        public void Process(ProductionRequestContext context)
        {
            context.ComponentMultiplier = 0;
            context.Request.IsValid = true;
            context.Request.Messages.Clear();
            context.Request.ProducingBatchNumber = context.Request.ProducingBatchNumber?.Trim();

            foreach (var component in context.Request.Components)
            {
                component.IsValid = true;
                component.Messages.Clear();
            }
        }
    }
}
