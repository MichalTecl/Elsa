using System;

using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Automation
{
    [Entity]
    public interface IJobSchedule : IProjectRelatedEntity
    {
        int Id { get; }
        
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
