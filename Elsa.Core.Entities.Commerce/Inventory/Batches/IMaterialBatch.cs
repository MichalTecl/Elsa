using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory.Batches
{
    [Entity]
    public interface IMaterialBatch : IProjectRelatedEntity
    {
        int Id { get; }

        int MaterialId { get; set; }
        IMaterial Material { get; }

        decimal Volume { get; set; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        int AuthorId { get; set; }
        IUser Author { get; }

        DateTime Created { get; set; }
        
        [NVarchar(64, true)]
        string BatchNumber { get; set; }

        [NVarchar(1024, false)]
        string Note { get; set; }

        DateTime? Expiration { get; set; }

        decimal Price { get; set; }

        [ForeignKey(nameof(IMaterialBatchComposition.CompositionId))]
        IEnumerable<IMaterialBatchComposition> Components { get; }

        DateTime? CloseDt { get; set; } 

        DateTime? LockDt { get; set; }

        int? LockUserId { get; set; }
        IUser LockUser { get; }

        [NVarchar(1024, true)]
        string LockReason { get; set; }
    }
}
