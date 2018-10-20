using System;

using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IMaterialStockEvent : IProjectRelatedEntity, IStockEventBase
    {
        int Id { get; }

        DateTime EventDt { get; set; }

        int UserId { get; set; }
        IUser User { get; }

        [NVarchar(1000, false)]
        string Description { get; set; }

        [NotFk]
        [NVarchar(255, true)]
        string BatchId { get; set; }
    }
}
