using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Automation
{
    [Entity]
    public interface IJobSchedule
    {
        int Id { get; }

        int ProjectId { get; set; }

        IProject Project { get; }

        int ScheduledJobId { get; set; }

        IScheduledJob ScheduledJob { get; }

        int? RunAfterId { get; set; }

        IJobSchedule RunAfter { get; }

        [NVarchar(4, true)]
        string StartTimeHhMm { get; set; }

        bool Active { get; set; }

        DateTime? LastStartDt { get; set; }

        DateTime? LastEndDt { get; set; }

        bool? LastRunFailed { get; set; }

        bool CanBeStartedManually { get; set; }

        int? RetryMinutes { get; set; }

        int? LoopLaunchPriority { get; set; }
    }
}
