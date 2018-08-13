using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.Common
{
    public interface IJobScheduler
    {
        JobSchedulerEntry Next();
    }
}
