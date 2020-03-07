using System;
using Robowire;

namespace Elsa.Users.Infrastructure
{
    public class UserRightsAttribute : Attribute, ISelfSetupAttribute
    {
        public void Setup(Type markedType, IContainerSetup setup)
        {
            UserRights.RegisterType(markedType);
        }
    }
}
