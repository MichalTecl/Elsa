using System;

using Elsa.Common.UserRightsInfrastructure;

using Robowire;

namespace Elsa.Common
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
