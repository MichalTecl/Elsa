using System;
using System.Collections.Generic;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.Core;

namespace Robowire.Plugin.DefaultPlugins
{
    internal class ConstructorInvocationBuilder : IPlugin
    {
        public bool IsApplicable(IServiceSetupRecord setup)
        {
            return
                !(setup.HasValueFactory || (setup.ImplementingType == null) || !setup.ImplementingType.IsClass
                  || setup.ImplementingType.IsAbstract); 
        }

        public INamedReference GenerateFactoryMethod(
            IServiceSetupRecord setup,
            Dictionary<string, INamedReference> ctorParamValueFactoryFields,
            INamedReference valueFactoryField,
            IClassBuilder locatorBuilder,
            INamedReference previousPluginMethod)
        {
            var implementingType = setup.ImplementingType ?? setup.InterfaceType;

            var methodName = $"Factory{TypeNameHelper.GetTypeMark(setup.InterfaceType)}_{Guid.NewGuid():N}";

            var method = locatorBuilder.HasMethod(methodName).WithModifier("private").Returns(setup.InterfaceType);

            method.Body.Write("return ").InvokeConstructor(implementingType,
                invocation =>
                    {
                        foreach (var constructorParameter in setup.PreferredConstructor.GetParameters())
                        {
                            INamedReference factoryField;
                            if (ctorParamValueFactoryFields.TryGetValue(constructorParameter.Name, out factoryField))
                            {
                                invocation.WithParam(b => b.Write("(").Write(constructorParameter.ParameterType).Write(")").Write(factoryField).Write("(this)"));
                            }
                            else
                            {
                                invocation.WithParam(b => b.Write("(").Write(constructorParameter.ParameterType).Write(")").Write("Get(typeof(").Write(constructorParameter.ParameterType).Write("))"));
                            }
                        }
                    }).EndStatement();

            return method;
        }

        public IPlugin InheritToChildContainer()
        {
            return this;
        }
    }
}
