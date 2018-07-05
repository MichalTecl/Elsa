using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;

using Robowire;
using Robowire.RobOrm.SqlServer;

namespace Elsa.Core.Entities.Commerce
{
    public static class ElsaDbInstaller
    {
        public static void Initialize(IContainer container)
        {
            RobOrmInitializer.Initialize(container, () => new ConnectionStringProvider(), true, typeof(IProject).Assembly);
        }
    }
}
