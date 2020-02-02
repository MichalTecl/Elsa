using System;
using Robowire;

namespace Elsa.Users.Infrastructure
{
    public class UserRightsDefinitionAttribute : Attribute, ISelfSetupAttribute
    {
        public UserRightsDefinitionAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public void Setup(Type markedType, IContainerSetup setup)
        {
            UserRightDefinitionCollector.RegisterType(markedType);
        }
    }
}
