using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CodeGeneration;
using CodeGeneration.Primitives;

namespace Robowire.Core.LocatorGeneration
{
    public class LocatorBuilder
    {
        public void BuildLocatorCode(IEnumerable<InstanceRecord> records, ICompiler compiler)
        {
            var recordsList = records.ToList();

            var locatorClass = new ClassBuilder($"Locator_{Guid.NewGuid():N}").WithModifier("public").WithModifier("sealed")
                .Inherits<CompiledLocatorBase>();

            var getMethod = locatorClass.OverridesMethod(typeof(CompiledLocatorBase).GetMethod("InternalGet", BindingFlags.NonPublic | BindingFlags.Instance));
            var typeParam = getMethod.WithParam("t", typeof(Type));

            var getCollectionItemsMethod = locatorClass.OverridesMethod(typeof(CompiledLocatorBase).GetMethod("GetCollectionItems", BindingFlags.NonPublic | BindingFlags.Instance));

            IConstructorBuilder ctor;
            
            var allFactoryFields = ProjectFactories(recordsList, locatorClass, out ctor);
            
            foreach (var record in recordsList)
            {
                var factoryMethod = CreateFactoryMethod(locatorClass, record, allFactoryFields);

                //var instanceField = locatorClass.HasField(record.InterfaceType);

                getMethod.Body.If(
                    condition =>
                        condition.Write(typeParam)
                            .Write(" == ")
                            .Write("typeof(")
                            .Write(record.InterfaceType)
                            .Write(")"),
                    then => then.Write("return ").Invoke(factoryMethod, i => { }).EndStatement().NewLine());

                //.LazyReadOrAssign(instanceField, assignment => assignment.Invoke("TryRegisterDisposable", invo => invo.WithParam(code => code.Invoke(factoryMethod, i => { })))).EndStatement()).NewLine();
            }

            getMethod.Body.If(
                condition =>
                    condition.Write(typeParam)
                        .Write(" == ")
                        .Write("typeof(")
                        .Write(typeof(IServiceLocator))
                        .Write(")"), then => then.Write("return this").EndStatement());

            getMethod.Body.Write("return TryGetFromParent(").Write(typeParam).Write(")").EndStatement();

            locatorClass.OverridesMethod(typeof(CompiledLocatorBase).GetMethod("CreateLocatorInstance"))
                .Body.Write("return ")
                .InvokeConstructor(
                    ctor,
                    inv => inv.WithParam(cb => cb.Write("parentLocator")).WithParam(cb => cb.Write("factories")))
                .EndStatement();

            getCollectionItemsMethod.Body.Write("yield break").EndStatement();

            locatorClass.Render(compiler);
        }

        private Dictionary<string, INamedReference> ProjectFactories(IEnumerable<InstanceRecord> records, IClassBuilder locator, out IConstructorBuilder ctor)
        {
            var result = new Dictionary<string, INamedReference>();

            ctor = locator.WithConstructor().WithModifier("public");

            var parentLocatorParam = ctor.WithParam<IServiceLocator>("parentLocator");
            ctor.CallsBase().WithParam(parentLocatorParam);

            var factoriesParam = ctor.WithParam("factories", typeof(Dictionary<string, Func<IServiceLocator, object>>));

            var constructor = ctor;

            Action<NamedFactory> addFactory = factory =>
                {
                    var field =
                        locator.HasField<Func<IServiceLocator, object>>()
                            .WithModifier("readonly")
                            .WithModifier("private");

                    constructor.Body.Assign(
                        field,
                        a => a.Write(factoriesParam).Write("[\"").Write(factory.Name).Write("\"]"));

                    result.Add(factory.Name, field);
                };

            foreach (var record in records)
            {
                if (record.Factory != null)
                {
                    addFactory(record.Factory);
                }

                if (record.ConstructorParameters != null)
                {
                    foreach (var ctorFactory in record.ConstructorParameters.Where(p => p.ValueProvider != null).Select(p => p.ValueProvider))
                    {
                        addFactory(ctorFactory);
                    }
                }
            }

            return result;
        }

        private INamedReference CreateFactoryMethod(IClassBuilder locator, InstanceRecord record, Dictionary<string, INamedReference> allFactoryFields)
        {
            var ctorParamFactoryFields = new Dictionary<string, INamedReference>();

            if (record.ConstructorParameters != null)
            {
                foreach (var ctorParam in record.ConstructorParameters)
                {
                    if (ctorParam.ValueProvider != null)
                    {
                        ctorParamFactoryFields[ctorParam.ParameterName] = allFactoryFields[ctorParam.ValueProvider.Name];
                    }
                }
            }

            INamedReference valueFactoryField = null;
            if (record.Factory != null)
            {
                valueFactoryField = allFactoryFields[record.Factory.Name];
            }
            

            INamedReference lastFactoryMethod = null;
            foreach (var plugin in record.ApplicablePlugins)
            {
                lastFactoryMethod = plugin.GenerateFactoryMethod(
                    record,
                    ctorParamFactoryFields,
                    valueFactoryField,
                    locator,
                    lastFactoryMethod);
            }

            if (lastFactoryMethod == null)
            {
                throw new InvalidOperationException($"Not any plugin was able to implement factory for {record.InterfaceType}");
            }

            return lastFactoryMethod;
        }
    }
}
