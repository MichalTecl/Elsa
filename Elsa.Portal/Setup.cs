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

            container.Setup(s => s.For<IWebSession>().Use<UserWebSession>());
            container.Setup(s => s.For<ISession>().Import.FromFactory(l => l.Get<IWebSession>()));
        }
    }
}