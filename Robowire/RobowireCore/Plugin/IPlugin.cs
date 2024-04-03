using System.Collections.Generic;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.Core;

namespace Robowire.Plugin
{
    public interface IPlugin
    {
        bool IsApplicable(IServiceSetupRecord setup);

        INamedReference GenerateFactoryMethod(
            IServiceSetupRecord setup,
            Dictionary<string, INamedReference> ctorParamValueFactoryFields,
            INamedReference valueFactoryField,
            IClassBuilder locatorBuilder,
            INamedReference previousPluginMethod);
        
        IPlugin InheritToChildContainer();
    }
}
