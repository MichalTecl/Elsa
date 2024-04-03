using System;
using System.Collections.Generic;
using System.Reflection;

using Robowire.Plugin;
using Robowire.Plugin.Flow;

namespace Robowire
{
    public interface IContainerSetup
    {
        IImplementingTypeSetup<T> For<T>();

        IImplementingTypeSetup For(Type interfaceType);

        IContainerSetup RegisterPlugin(Action<IPluginCollection> setup);

        IContainerSetup Collect<T>();

        IContainerSetup Collect(Type baseType);

        IContainerSetup SubscribeCodeGeneratorListener(IGeneratedCodeListener listener);

        IContainerSetup ScanAssembly(Assembly assembly);

        IContainerSetup ScanType(Type t);

        IContainerSetup ScanType<T>();

        IEnumerable<T> GetRegisteredPlugins<T>() where T : IPlugin;
    }
}
