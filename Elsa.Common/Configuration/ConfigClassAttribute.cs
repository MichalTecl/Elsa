using System;
using Elsa.Common.Interfaces;
using Robowire;

namespace Elsa.Common.Configuration
{
    public class ConfigClassAttribute : Attribute, ISelfSetupAttribute
    {
        public void Setup(Type markedType, IContainerSetup setup)
        {
            setup.For(markedType).ImportObject.FromFactory(locator => ConfigFactory(locator, markedType));
        }

        private static object ConfigFactory(IServiceLocator locator, Type requestedType)
        {
            var session = locator.Get<ISession>();

            var projectId = session?.Project?.Id;
            var userId = session?.User?.Id;
            
            var repo = locator.Get<IConfigurationRepository>();

            return repo.Load(requestedType, projectId, userId);
        }
    }
}
