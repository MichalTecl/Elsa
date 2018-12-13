using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

namespace Elsa.Core.Entities.Commerce.Extensions
{
    public static class ProductionStepExtensions
    {
        public static IEnumerable<IMaterialProductionStep> Ordered(this IEnumerable<IMaterialProductionStep> steps)
        {
            var all = steps.ToList();
            if (all.Count == 0)
            {
                yield break;
            }

            int? previousStepId = null;

            while (all.Count > 0)
            {
                var cStep = all.FirstOrDefault(s => s.PreviousStepId == previousStepId) ?? all.First();

                yield return cStep;
                previousStepId = cStep.Id;

                all.Remove(cStep);
            }
        }
    }
}
