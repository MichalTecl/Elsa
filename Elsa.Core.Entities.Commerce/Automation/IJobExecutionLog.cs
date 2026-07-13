using System;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Automation
{
    [Entity]
    public interface IJobExecutionLog : IIntIdEntity
    {
        int ScheduledJobId { get; set; }

        IScheduledJob ScheduledJob { get; }

        int StartUserId { get; set; }

        IUser StartUser { get; }

        DateTime StartDt { get; set; }

        DateTime? EndDt { get; set; }

        [NVarchar(NVarchar.Max, true)]
        string ErrorMessage { get; set; }
    }
}
