using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Core.Entities.Commerce.Common.SystemCounters;

namespace Elsa.Common.SysCounters
{
    public interface ISysCountersManager
    {
        void WithCounter(int counterTypeId, Action<string> newValueCallback);

        ISystemCounter GetCounter(int id);
    }
}
