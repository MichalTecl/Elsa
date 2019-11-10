using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model.BatchPriceExpl;
using Elsa.Commerce.Core.Units;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.Model
{
    public interface IMaterialBatchStatus
    {
        List<IMaterialStockEvent> Events { get; }
        
        List<IOrderItemMaterialBatch> UsedInOrderItems { get; }
        
        List<IMaterialBatchComposition> UsedInCompositions { get; }
       
     
        Amount CurrentAvailableAmount { get; }
        
        BatchPrice BatchPrice { get; }
        
        Amount CalculateAvailableAmount(AmountProcessor amountProcessor);
    }
}
