using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Automation;

namespace Elsa.Jobs.Common
{
    public interface IScheduledJobsRepository
    {
        IScheduledJob GetCurrentJob(int projectId);
    }
}
