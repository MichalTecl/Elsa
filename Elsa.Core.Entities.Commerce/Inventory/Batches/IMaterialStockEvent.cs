using System;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory.Batches
{
    [Entity]
    public interface IMaterialStockEvent : IProjectRelatedEntity
    {
        int Id { get; }

        int BatchId { get; set; }
        IMaterialBatch Batch { get; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        decimal Delta { get; set; }
        
        int TypeId { get; set; }
        IStockEventType Type { get; }

        [NVarchar(0, false)]
        string Note { get; set; }

        int UserId { get; set; }
        IUser User { get; }

        DateTime EventDt { get; set; }

        [NVarchar(32, false)]
        string EventGroupingKey { get; set; }

        long? SourcePurchaseOrderId { get; set; }
        IPurchaseOrder SourcePurchaseOrder { get; }
    }
}
