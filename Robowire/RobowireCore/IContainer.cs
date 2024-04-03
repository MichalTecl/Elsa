using System;

using Robowire.Plugin.Flow;

namespace Robowire
{
    public interface IContainer
    {
        IContainer Parent { get; }

        IReadOnlyPluginCollection Plugins { get; }

        IServiceLocator GetLocator();

        IContainer Setup(Action<IContainerSetup> setup);
    }
}
