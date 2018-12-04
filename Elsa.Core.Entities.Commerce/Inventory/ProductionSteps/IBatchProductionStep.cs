using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.ProductionSteps
{
    [Entity]
    public interface IBatchProductionStep
    {
        int Id { get; }

        int StepId { get; set; }

        IMaterialProductionStep Step { get; }

        int BatchId { get; set; }
        IMaterialBatch Batch { get; }

        decimal ProducedAmount { get; set; }

        decimal? Price { get; set; }

        int ConfirmUserId { get; set; }
        IUser ConfirmUser { get; }

        DateTime ConfirmDt { get; set; }

        int? WorkerId { get; set; }
        IUser Worker { get; }

        decimal? SpentHours { get; set; }

        [ForeignKey(nameof(IBatchProuctionStepSourceBatch.StepId))]
        IEnumerable<IBatchProuctionStepSourceBatch> SourceBatches { get; }
    }
}
