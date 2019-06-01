using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory.Batches
{
    [Entity]
    public interface IMaterialBatch : IProjectRelatedEntity, IVolumeAndUnit
    {
        int Id { get; }

        int MaterialId { get; set; }
        IMaterial Material { get; }
        
        int AuthorId { get; set; }
        IUser Author { get; }

        DateTime Created { get; set; }
        
        [NVarchar(64, true)]
        string BatchNumber { get; set; }

        [NVarchar(1024, false)]
        string Note { get; set; }

        DateTime? Expiration { get; set; }

        decimal Price { get; set; }

        decimal? ProductionWorkPrice { get; set; }

        int? PriceConversionId { get; set; }
        ICurrencyConversion PriceConversion { get; }

        [ForeignKey(nameof(IMaterialBatchComposition.CompositionId))]
        IEnumerable<IMaterialBatchComposition> Components { get; }

        DateTime? CloseDt { get; set; } 

        DateTime? LockDt { get; set; }

        int? LockUserId { get; set; }
        IUser LockUser { get; }

        [NVarchar(1024, true)]
        string LockReason { get; set; }

        bool IsAvailable { get; set; }

        DateTime? Produced { get; set; }

        [NVarchar(100, true)]
        string InvoiceNr { get; set; }

        [NVarchar(100, true)]
        string InvoiceVarSymbol { get; set; }

        [ForeignKey(nameof(IBatchProductionStep.BatchId))]
        IEnumerable<IBatchProductionStep> PerformedSteps { get; }

        bool? AllStepsDone { get; set; }

        int? SupplierId { get; set; }
        ISupplier Supplier { get; }
    }
}
