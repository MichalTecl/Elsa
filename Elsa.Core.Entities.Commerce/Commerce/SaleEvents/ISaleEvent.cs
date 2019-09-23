using System;
using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce.SaleEvents
{
    [Entity]
    public interface ISaleEvent : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(500, false)]
        string Name { get; set; }

        DateTime EventDt { get; set; }

        int UserId { get; set; }

        IUser User { get; }
        IEnumerable<ISaleEventAllocation> Allocations { get; }
    }
}
