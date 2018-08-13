using System;

using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Automation
{
    [Entity]
    public interface IJobExecutionHistory
    {
        long Id { get; }

        int ScheduledJobId { get; set; }

        IScheduledJob ScheduledJob { get; }

        DateTime LastStartDt { get; set; }

        DateTime? LastEndDt { get; set; }

        [NVarchar(256, true)]
        string ErrorMessage { get; set; }

        int ExecUserId { get; set; }

        IUser ExecUser { get; }
    }
}
