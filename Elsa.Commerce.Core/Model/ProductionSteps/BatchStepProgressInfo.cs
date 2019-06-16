using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

namespace Elsa.Commerce.Core.Model.ProductionSteps
{
    public class BatchStepProgressInfo
    {
        public BatchStepProgressInfo(IMaterialProductionStep requiredStep, Amount requiredAmount, Amount totalProducedAmount, List<IBatchProductionStep> performedSteps)
        {
            RequiredStep = requiredStep;
            RequiredAmount = requiredAmount;
            TotalProducedAmount = totalProducedAmount;
            PerformedSteps = performedSteps;
        }

        public IMaterialProductionStep RequiredStep { get; }

        public Amount RequiredAmount { get; }

        public Amount TotalProducedAmount { get; }

        public List<IBatchProductionStep> PerformedSteps { get; }
    }
}
