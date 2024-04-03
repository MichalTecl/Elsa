using System;
using System.Collections.Generic;
using System.Linq;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.Core;
using Robowire.Plugin;

namespace Robowire.RoboApi.Internal
{
    public class ControllerCollectorPlugin : IPlugin
    {
        public bool IsApplicable(IServiceSetupRecord setup)
        {
            return setup.Behaviors.OfType<ControllerBehavior>().Any();
        }

        public INamedReference GenerateFactoryMethod(
            IServiceSetupRecord setup,
            Dictionary<string, INamedReference> ctorParamValueFactoryFields,
            INamedReference valueFactoryField,
            IClassBuilder locatorBuilder,
            INamedReference previousPluginMethod)
        {
            var behavior = setup.Behaviors.OfType<ControllerBehavior>().FirstOrDefault();
            if (behavior == null)
            {
                return previousPluginMethod;
            }

            var indexFactory =
                locatorBuilder.HasMethod("GetRoboapiControllerIndexFactory")
                    .Returns<IEnumerable<KeyValuePair<string, Type>>>()
                    .WithModifier("private");

            indexFactory.Body.Write("yield return new ")
                .Write(typeof(KeyValuePair<string, Type>))
                .Write("(")
                .String(behavior.ControllerName)
                .Write(", ")
                .Typeof(setup.ImplementingType)
                .Write(")")
                .EndStatement();

            var builder = Activator.CreateInstance(behavior.ProxyBuilderType) as ControllerProxyBuilder;

            var proxy = builder.BuildProxyClass(locatorBuilder, setup.ImplementingType, behavior.CallBuilderType);

            var factoryMethod =
                locatorBuilder.HasMethod($"factory{setup.ImplementingType.Name}").Returns(setup.ImplementingType);

            factoryMethod.Body.Write("return new ").Write(proxy).Write("(this)").EndStatement();

            return factoryMethod;
        }


        public IPlugin InheritToChildContainer()
        {
            return new ControllerCollectorPlugin();
        }
    }
}
