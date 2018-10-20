using System;

using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IStockLevelSnapshot : IProjectRelatedEntity, IStockEventBase
    {
        long Id { get; }

        DateTime SnapshotDt { get; set; }

        bool IsManual { get; set; }

        int UserId { get; set; }
        IUser User { get; }

        [NVarchar(1000, true)]
        string Note { get; set; }
    }
}
