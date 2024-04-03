using System.Collections.Generic;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.Core;
using Robowire.Plugin;

namespace Robowire.RobOrm.Core.EntityGeneration
{
    public class EntityCollectorPlugin : IPlugin
    {
        public bool IsApplicable(IServiceSetupRecord setup)
        {
            return setup.InterfaceType == typeof(IEntityCollector);
        }

        public INamedReference GenerateFactoryMethod(
            IServiceSetupRecord setup,
            Dictionary<string, INamedReference> ctorParamValueFactoryFields,
            INamedReference valueFactoryField,
            IClassBuilder locatorBuilder,
            INamedReference previousPluginMethod)
        {

            var factory = locatorBuilder.HasMethod("GetEntityCollectorInstance").Returns(typeof(IEntityCollector));
            factory.WithModifier("private");

            factory.Body.Write("return new ")
                .Write(typeof(EntityCollector))
                .Write("(System.Linq.Enumerable.ToList(this.GetEntitiesCollection()))")
                .EndStatement();
            
            return factory;
        }

        public IPlugin InheritToChildContainer()
        {
            return this;
        }
    }
}
