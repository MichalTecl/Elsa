using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Utils
{
    public class TimeoutCheck
    {
        public TimeoutCheck(TimeSpan timeout)
        {
            Timeout = timeout;
            Started = DateTime.Now;
        }

        public TimeSpan Timeout { get; }
        public DateTime Started { get; }

        public void Check()
        {
            if ((DateTime.Now - Started) > Timeout)
                throw new TimeoutException();
        }
    }
}
