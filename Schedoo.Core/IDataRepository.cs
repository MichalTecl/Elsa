using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schedoo.Core
{
    public interface IDataRepository
    {
        IEnumerable<IJob> AllJobs { get; }
            
        DateTime Now { get; }

        void OnJobStart(IJob job);

        void OnJobSucceeded(IJob job);

        void OnJobFailed(IJob job, Exception ex);

        DateTime? GetLastJobStarted(IJob job);

        DateTime? GetLastJobSucceeded(IJob job);

        DateTime? GetLastJobFailed(IJob job);
    }
}
