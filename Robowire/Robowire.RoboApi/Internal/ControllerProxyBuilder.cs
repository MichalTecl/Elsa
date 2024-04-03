using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.RoboApi.Convention;
using Robowire.RoboApi.Extensibility;

namespace Robowire.RoboApi.Internal
{
    public class ControllerProxyBuilder
    {
        public virtual INamedReference BuildProxyClass(IClassBuilder locatorBuilder, Type controllerType, Type callBuilderType)
        {
            var proxy = locatorBuilder.HasNestedClass($"ControllerProxy_{controllerType.Name}_{Guid.NewGuid():N}");

            proxy.Inherits(controllerType);
            proxy.Implements<ILocatorBoundController>();

            var proxyCtor = proxy.WithConstructor().WithModifier("public");
            var locatorRef = proxyCtor.WithParam("__locator", typeof(IServiceLocator));

            var locatorField = proxy.HasField<IServiceLocator>().WithModifier("private").WithModifier("readonly");

            var baseCall = proxyCtor.CallsBase();
            
            var baseCtor = controllerType.GetConstructors().FirstOrDefault();

            proxyCtor.Body.Assign(locatorField, a => a.Write(locatorRef));

            var interceptorField = proxy.HasField(typeof(IControllerInterceptor)).WithModifier("private").WithModifier("readonly");

            proxyCtor.Body.Assign(
                interceptorField,
                asg =>
                    asg.Write(typeof(InterceptorProvider))
                        .Write(".")
                        .Write(nameof(InterceptorProvider.GetInterceptor))
                        .Write("(this)"));

            foreach (var baseCtorParam in baseCtor.GetParameters())
            {
                baseCall.WithParam(
                    p => p.Write(locatorRef)
                          .Write(".Get<")
                          .Write(baseCtorParam.ParameterType)
                          .Write(">()"));
            }

            proxy.HasProperty<IServiceLocator>(nameof(ILocatorBoundController.Locator)).WithModifier("public").HasGetter().Returns(w => w.Write(locatorField));
            
            AddDependencies(locatorBuilder, proxyCtor, controllerType);

            ImplementExecuteMethod(proxy, controllerType, callBuilderType, interceptorField);

            return proxy;
        }

        protected virtual void AddDependencies(IClassBuilder locatorBuilder, IConstructorBuilder proxyCtor, Type controllerType)
        {
        }

        protected virtual void ImplementExecuteMethod(IClassBuilder proxyclass, Type controllerType, Type callBuilderType, INamedReference interceptorField)
        {
            var execMethod = proxyclass.HasMethod(nameof(IController.Execute)).WithModifier("public").ReturnsVoid();
            
            var contextParam = execMethod.WithParam("__context", typeof(RequestContext));
            
            execMethod.Body.If(
                condition =>
                    condition.Invoke(
                        interceptorField,
                        nameof(IControllerInterceptor.OnRequest),
                        inv => inv.WithParam("this").WithParam(contextParam)),
                then => then.Write("return").EndStatement());

            var factories = new Dictionary<Type, INamedReference>();

            Func<Type, INamedReference> fieldsFactory = type =>
                {
                    INamedReference result;
                    if (!factories.TryGetValue(type, out result))
                    {
                        result =
                            proxyclass.HasField(type)
                                .WithModifier("private")
                                .WithModifier("static")
                                .WithModifier("readonly")
                                .WithAssignment(asg => asg.Write("new ").Write(type).Write("()"));
                        factories[type] = result;
                    }

                    return result;
                };
            
            var writer = Activator.CreateInstance(callBuilderType) as IControllerMethodCallBuilder;
            if (writer == null)
            {
                throw new InvalidOperationException($"{callBuilderType.Name} is not of type {nameof(IControllerMethodCallBuilder)}");
            }

            writer.BuildCall(proxyclass, execMethod, contextParam, controllerType, fieldsFactory, interceptorField);
        }
    }
}
