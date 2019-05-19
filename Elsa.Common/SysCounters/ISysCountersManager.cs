using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.SysCounters
{
    public interface ISysCountersManager
    {
        void WithCounter(int counterTypeId, Action<string> newValueCallback);
    }
}
