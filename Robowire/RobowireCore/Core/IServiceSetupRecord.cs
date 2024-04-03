using System;
using System.Collections.Generic;
using System.Reflection;

using Robowire.Plugin;

namespace Robowire.Core
{
    public interface IServiceSetupRecord
    {
        Type InterfaceType { get; }

        Type ImplementingType { get; }

        NamedFactory Factory { get; }

        bool HasValueFactory { get; }

        IEnumerable<CtorParamSetupRecord> ConstructorParameters { get; }

        IEnumerable<IPlugin> ApplicablePlugins { get; }

        ConstructorInfo PreferredConstructor { get; }

        IEnumerable<IBehavior> Behaviors { get; }
    }
}
