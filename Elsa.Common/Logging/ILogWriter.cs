using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common.Logging;

namespace Elsa.Common.Logging
{
    public interface ILogWriter
    {
        void Write(ISysLog entry);
    }
}
