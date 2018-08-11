using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Elsa.Common;
using Elsa.Users;

using Robowire;

namespace Elsa.Portal
{
    public static class Setup
    {
        public static void SetupContainer(IContainer container)
        {
            CommonRegistry.SetupContainer(container);

            container.Setup(s => s.For<ISession>().Use<UserSession>());
        }
    }
}