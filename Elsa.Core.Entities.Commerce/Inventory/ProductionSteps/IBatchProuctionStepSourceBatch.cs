using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.ProductionSteps
{
    [Entity]
    public interface IBatchProuctionStepSourceBatch
    {
        int Id { get; }

        int StepId { get; set; }
        IBatchProductionStep Step { get; }

        int SourceBatchId { get; set; }
        IMaterialBatch SourceBatch { get; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        decimal UsedAmount { get; set; }
    }
}
