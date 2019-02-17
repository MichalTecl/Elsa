using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Units;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

namespace Elsa.Commerce.Core.Model
{
    public interface IMaterialBatchStatus
    {
        List<IMaterialProductionStep> RequiredSteps { get; }
        List<IBatchProductionStep> ResolvedSteps { get; }
        
        List<IMaterialStockEvent> Events { get; }
        
        List<IOrderItemMaterialBatch> UsedInOrderItems { get; }
        
        List<IMaterialBatchComposition> UsedInCompositions { get; }
        
        List<IBatchProuctionStepSourceBatch> UsedInSteps { get; } 
        
        Amount CurrentAvailableAmount { get; }

        Amount CalculateAvailableAmount(AmountProcessor amountProcessor, int filteredStepId, bool pretendAllStepsDone = false);
    }
}
