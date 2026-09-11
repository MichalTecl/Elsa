using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;

namespace Elsa.Core.Entities.Commerce.Integration
{
    [Entity]
    public interface IOrderImportFailure : IIntIdEntity
    {
        int ErpId { get; set; }

        IErp Erp { get; }

        [NVarchar(64, false)]
        string OrderNumber { get; set; }

        DateTime FirstFailureDt { get; set; }

        DateTime LastFailureDt { get; set; }

        DateTime? ResolveDate { get; set; }

        DateTime? EmailNotificationDt { get; set; }

        int FailureCount { get; set; }

        [NVarchar(-1, false)]
        string LastError { get; set; }
    }
}
