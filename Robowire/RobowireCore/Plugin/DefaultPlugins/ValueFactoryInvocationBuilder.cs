using System;
using System.Collections.Generic;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.Core;

namespace Robowire.Plugin.DefaultPlugins
{
    internal sealed class ValueFactoryInvocationBuilder : IPlugin
    {
        public bool IsApplicable(IServiceSetupRecord setup)
        {
            return setup.HasValueFactory;
        }

        public INamedReference GenerateFactoryMethod(
            IServiceSetupRecord setup,
            Dictionary<string, INamedReference> ctorParamValueFactoryFields,
            INamedReference valueFactoryField,
            IClassBuilder locatorBuilder,
            INamedReference previousPluginMethod)
        {
            var methodName = $"Factory{TypeNameHelper.GetTypeMark(setup.InterfaceType)}_{Guid.NewGuid():N}";

            var method = locatorBuilder.HasMethod(methodName).WithModifier("private").Returns(setup.InterfaceType);

            method.Body.Write("return ").Write("(").Write(setup.InterfaceType).Write(")").Write(valueFactoryField).Write("(this)").EndStatement();

            return method;
        }

        public IPlugin InheritToChildContainer()
        {
            return this;
        }
    }
}
