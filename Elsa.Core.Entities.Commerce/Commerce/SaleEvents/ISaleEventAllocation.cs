using System;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Commerce.SaleEvents
{
    [Entity]
    public interface ISaleEventAllocation : IIntIdEntity
    {
        int SaleEventId { get; set; }

        int BatchId { get; set; }

        decimal AllocatedQuantity { get; set; }

        decimal? ReturnedQuantity { get; set; }

        int UnitId { get; set; }

        DateTime AllocationDt { get; set; }

        DateTime? ReturnDt { get; set; }

        int AllocationUserId { get; set; }
        int? ReturnUserId { get; set; }

        IMaterialBatch Batch { get; }
        IMaterialUnit Unit { get; }
        IUser AllocationUser { get; }
        IUser ReturnUser { get; }
        ISaleEvent SaleEvent { get; }
    }
}
