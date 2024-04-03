using System;
using System.Collections.Generic;
using System.Reflection;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.Core;

namespace Robowire.Plugin.DefaultPlugins
{
    public abstract class InterfaceImplementorBase : IPlugin
    {
        //protected abstract void 
        public bool IsApplicable(IServiceSetupRecord setup)
        {
            if (setup.HasValueFactory || !setup.InterfaceType.IsInterface)
            {
                return false;
            }

            return IsApplicable(setup.InterfaceType, setup.ImplementingType);
        }

        public INamedReference GenerateFactoryMethod(
            IServiceSetupRecord setup,
            Dictionary<string, INamedReference> ctorParamValueFactoryFields,
            INamedReference valueFactoryField,
            IClassBuilder locatorBuilder,
            INamedReference previousPluginMethod)
        {
            var implClass =
                locatorBuilder.HasNestedClass(
                        $"{TypeNameHelper.GetTypeMark(this.GetType())}_implements_{TypeNameHelper.GetTypeMark(setup.InterfaceType)}_{Guid.NewGuid():N}")
                    .Implements(setup.InterfaceType)
                    .WithModifier("private")
                    .WithModifier("sealed");

            var locatorField = implClass.HasField<IServiceLocator>();
            var locatorParam = implClass.WithConstructor().WithModifier("public").WithParam("locator", typeof(IServiceLocator));
            implClass.WithConstructor().Body.Assign(locatorField, a => a.Write(locatorParam));

            foreach (var property in setup.InterfaceType.GetProperties())
            {
                if (!ImplementProperty(implClass, property, locatorField))
                {
                    DefaultImplementProperty(implClass, property);
                }
            }

            foreach (var method in setup.InterfaceType.GetMethods())
            {
                if (method.IsSpecialName)
                {
                    continue;
                }

                if (!ImplementMethod(implClass, method, locatorField))
                {
                    DefaultImplementMethod(implClass, method);
                }
            }

            var factoryMethod =
                locatorBuilder.HasMethod($"factory_{TypeNameHelper.GetTypeMark(setup.InterfaceType)}_{Guid.NewGuid():N}").Returns(setup.InterfaceType).WithModifier("private");

            factoryMethod.Body.Write(" return ")
                .InvokeConstructor(implClass, i => i.WithParam(p => p.Write("this")))
                .EndStatement();

            return factoryMethod;
        }

        public IPlugin InheritToChildContainer()
        {
            return InheritForChildContainer();
        }

        private void DefaultImplementProperty(IClassBuilder cls, PropertyInfo property)
        {
            var storageField = cls.HasField($"storage{property.Name}_{Guid.NewGuid():N}", property.PropertyType);

            cls.WithConstructor()
                .Body.Assign(
                    storageField,
                    assignment => assignment.Write("default(").Write(property.PropertyType).Write(")"));
            
            var prop = cls.HasProperty(property.Name, property.PropertyType).WithModifier("public");

            prop.HasGetter().Write("return ").Write(storageField).EndStatement();
            prop.HasSetter().Assign(storageField, a => a.Write("value"));
        }

        private void DefaultImplementMethod(IClassBuilder cls, MethodInfo method)
        {
            cls.ImplementsMethod(method)
                .Body.Write("throw new ")
                .Write(typeof(NotImplementedException))
                .Write("()")
                .EndStatement();
        }

        protected abstract bool IsApplicable(Type interfaceType, Type implementingType);

        protected abstract bool ImplementProperty(IClassBuilder impl, PropertyInfo propertyInfo, INamedReference serviceLocatorField);

        protected abstract bool ImplementMethod(IClassBuilder impl, MethodInfo method, INamedReference serviceLocatorField);

        protected abstract InterfaceImplementorBase InheritForChildContainer();
    }
}
