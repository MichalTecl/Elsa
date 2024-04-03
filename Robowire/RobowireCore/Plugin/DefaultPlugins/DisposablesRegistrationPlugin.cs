using System;
using System.Collections.Generic;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.Behavior;
using Robowire.Core;

namespace Robowire.Plugin.DefaultPlugins
{
    public class DisposablesRegistrationPlugin : IPlugin
    {
        private static readonly DisposeBehavior s_defaultBehavior = new DisposeBehavior();

        public bool IsApplicable(IServiceSetupRecord setup)
        {
            Func<Type, bool> canBeDisposable = t => (t == null) || typeof(IDisposable).IsAssignableFrom(t);

            return setup.HasValueFactory || canBeDisposable(setup.ImplementingType) || canBeDisposable(setup.InterfaceType);
        }

        public INamedReference GenerateFactoryMethod(
            IServiceSetupRecord setup,
            Dictionary<string, INamedReference> ctorParamValueFactoryFields,
            INamedReference valueFactoryField,
            IClassBuilder locatorBuilder,
            INamedReference previousPluginMethod)
        {
            if (!setup.GetBehavior(s_defaultBehavior).Dispose)
            {
                return previousPluginMethod;
            }

            var method = locatorBuilder.HasMethod($"Dispose{TypeNameHelper.GetTypeMark(setup.InterfaceType)}_{Guid.NewGuid():N}")
                .Returns(setup.InterfaceType);

            method.Body.Write("return ")
                .Invoke("TryRegisterDisposable", invocation => invocation.WithParam(p => p.Invoke(previousPluginMethod,
                        inv => { })))
                .EndStatement();

            return method;
        }

        public IPlugin InheritToChildContainer()
        {
            return this;
        }
    }
}
