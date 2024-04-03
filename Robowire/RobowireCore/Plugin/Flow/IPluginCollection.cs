using System.Collections.Generic;

namespace Robowire.Plugin.Flow
{
    public interface IPluginCollection
    {
        IEnumerable<IPlugin> AllPlugins { get; }

        /// <summary>
        /// 0 or 1 can be applicable.  More than 1 applicable throws exception.
        /// If custom instance creator is applicable, DefaultInstanceCreators will be skipped
        /// </summary>   
        IList<IPlugin> CustomInstanceCreators { get; }

        /// <summary>
        /// If not any Custom Instance Creator is applicable, one of Default Instance Creators will resolve Constructor invocation, factory call or existing instance import 
        /// </summary>
        IList<IPlugin> DefaultInstanceCreators { get; }

        /// <summary>
        /// By default IDisposables registration, collections, property injection...
        /// </summary>
        IList<IPlugin> AfterNewInstanceCreated { get; }

        /// <summary>
        /// By default SingletonPlugin
        /// </summary>
        IList<IPlugin> LifecyclePlugins { get; }
    }

    public interface IReadOnlyPluginCollection
    {
        IEnumerable<IPlugin> CustomInstanceCreators { get; }

        IEnumerable<IPlugin> DefaultInstanceCreators { get; }

        IEnumerable<IPlugin> AfterNewInstanceCreated { get; }

        IEnumerable<IPlugin> LifecyclePlugins { get; }
    }
}
