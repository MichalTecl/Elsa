using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common.Logging;

namespace Elsa.Common.Logging
{
    public class NoActionLogWriter : ILogWriter
    {
        public void Write(ISysLog entry)
        {
        }
    }
}
