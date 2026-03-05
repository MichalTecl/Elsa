using Elsa.Common.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.CrmMailPull.Steps
{
    public interface IStep
    {
        void Run(TimeoutCheck timeout);
    }
}
