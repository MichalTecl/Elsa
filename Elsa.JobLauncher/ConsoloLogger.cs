using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common.Logging;

using Robowire.RobOrm.Core;

namespace Elsa.JobLauncher
{
    public class ConsoloLogger : Logger
    {
        public ConsoloLogger(IDatabase database, ISession session)
            : base(database, session)
        {
        }

        protected override void OnBeforeEntryEnqueue(ISysLog entry)
        {
            Console.WriteLine(entry.Message);
        }
    }
}
