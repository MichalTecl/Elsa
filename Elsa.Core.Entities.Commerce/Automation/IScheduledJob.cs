using System;
using System.Collections.Generic;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Automation
{
    [Entity]
    public interface IScheduledJob
    {
        int Id { get; }

        int ProjectId { get; set; }

        [NVarchar(64, false)]
        string Name { get; set; }
        
        int SecondsInterval { get; set; }

        int SequencePriority { get; set; }

        [NVarchar(256, false)]
        string ModuleClass { get; set; }

        [NVarchar(0, false)]
        string CustomData { get; set; }

        DateTime ActiveFrom { get; set; }
        
        DateTime? ActiveTo { get; set; }

        IEnumerable<IJobExecutionHistory> History { get; }
    }
}
