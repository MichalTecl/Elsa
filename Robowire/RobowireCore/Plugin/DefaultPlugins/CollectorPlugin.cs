using System;
using System.Collections.Generic;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.Core;

namespace Robowire.Plugin.DefaultPlugins
{
    public class CollectorPlugin : IPlugin
    {
        public readonly Type CollectionType;

        public CollectorPlugin(Type collectionType)
        {
            CollectionType = collectionType;
        }
        
        public bool IsApplicable(IServiceSetupRecord setup)
        {

            return ((setup.InterfaceType != null) && CollectionType.IsAssignableFrom(setup.InterfaceType))
                   || ((setup.ImplementingType != null) && CollectionType.IsAssignableFrom(setup.ImplementingType));
        }
        
        public INamedReference GenerateFactoryMethod(
            IServiceSetupRecord setup,
            Dictionary<string, INamedReference> ctorParamValueFactoryFields,
            INamedReference valueFactoryField,
            IClassBuilder locatorBuilder,
            INamedReference previousPluginMethod)
        {

            var collectionMethod = locatorBuilder.HasMethod("GetCollectionItems");

            collectionMethod.Body.If(
                condition => condition.Write("collectionType == typeof(").Write(CollectionType).Write(")"),
                then => then.Write("yield return Get(typeof(").Write(setup.InterfaceType).Write("))").EndStatement()).NewLine();

            return previousPluginMethod;
        }

        public IPlugin InheritToChildContainer()
        {
            return this;
        }
    }
}
