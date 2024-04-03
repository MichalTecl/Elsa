using System;
using System.Collections.Generic;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.Behavior;
using Robowire.Core;

namespace Robowire.Plugin.DefaultPlugins
{
    public class LifecyclePlugin : IPlugin
    {
        private static readonly LifecycleBehavior s_defaultBehavior = new LifecycleBehavior();

        public bool IsApplicable(IServiceSetupRecord setup)
        {
            return true;
        }

        public INamedReference GenerateFactoryMethod(
            IServiceSetupRecord setup,
            Dictionary<string, INamedReference> ctorParamValueFactoryFields,
            INamedReference valueFactoryField,
            IClassBuilder locatorBuilder,
            INamedReference previousPluginMethod)
        {
            if (setup.GetBehavior(s_defaultBehavior).AlwaysNewInstance)
            {
                return previousPluginMethod;
            }

            var field = locatorBuilder.HasField(setup.InterfaceType).WithModifier("private");

            var fMethod = locatorBuilder.HasMethod($"Singleton_{TypeNameHelper.GetTypeMark(setup.InterfaceType)}_{Guid.NewGuid():N}").Returns(setup.InterfaceType);

            fMethod.Body.Write(" return ")
                .LazyReadOrAssign(field, a => a.Invoke(previousPluginMethod, inv => { }))
                .EndStatement();

            return fMethod;
        }

        public IPlugin InheritToChildContainer()
        {
            return this;
        }
    }
}
