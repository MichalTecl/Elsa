using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.ScheduledJobs.Model
{
    public class ScheduledJobStatus
    {
        public int ScheduleId { get; set; }

        public string Name { get; set; }

        public string CurrentStatus { get; set; }
        
        public string StartMode { get; set; }

        public string LastRun { get; set; }

        public bool CanBeStarted { get; set; }
    }
}
