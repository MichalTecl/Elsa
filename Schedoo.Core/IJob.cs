using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schedoo.Core
{
    public interface IJob
    {
        int Priority { get; }

        string Uid { get; }

        TimeSpan RunTimeout { get; }

        bool EvalPreconditions(IJobContext c);
    }
}
