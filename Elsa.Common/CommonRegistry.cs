using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce;

using Robowire;
using Robowire.RobOrm.SqlServer;

namespace Elsa.Common
{
    public static class CommonRegistry
    {
        public static void SetupContainer(IContainer container)
        {
            ElsaDbInstaller.Initialize(container);
        }

    }
}
