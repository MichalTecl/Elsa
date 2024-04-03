using System;
using System.Collections.Generic;

using Robowire.Plugin.DefaultPlugins;

namespace Robowire.Plugin.Flow
{
    internal class PluginCollection : IPluginCollection, IReadOnlyPluginCollection
    {
        private readonly List<IPlugin> m_customInstanceCreators = new List<IPlugin>();
        private readonly List<IPlugin> m_defaultInstanceCreators = new List<IPlugin>();
        private readonly List<IPlugin> m_afterNewInstanceCreated = new List<IPlugin>();
        private readonly List<IPlugin> m_lifecyclePlugins = new List<IPlugin>();

        public PluginCollection()
        {
            DefaultInstanceCreators.Add(new ConstructorInvocationBuilder());
            DefaultInstanceCreators.Add(new ValueFactoryInvocationBuilder());
            AfterNewInstanceCreated.Add(new DisposablesRegistrationPlugin());
            LifecyclePlugins.Add(new LifecyclePlugin());
        }

        public PluginCollection(IReadOnlyPluginCollection parent)
        {
            Action<IEnumerable<IPlugin>, IList<IPlugin>> inherit = (source, target) =>
                {
                    foreach (var srcPlugin in source)
                    {
                        target.Add(srcPlugin.InheritToChildContainer());
                    }
                };

            inherit(parent.CustomInstanceCreators, m_customInstanceCreators);
            inherit(parent.DefaultInstanceCreators, m_defaultInstanceCreators);
            inherit(parent.AfterNewInstanceCreated, m_afterNewInstanceCreated);
            inherit(parent.LifecyclePlugins, m_lifecyclePlugins);
        }

        public IEnumerable<IPlugin> AllPlugins
        {
            get
            {
                foreach (var p in CustomInstanceCreators) yield return p;
                foreach (var p in DefaultInstanceCreators) yield return p;
                foreach (var p in AfterNewInstanceCreated) yield return p;
                foreach (var p in LifecyclePlugins) yield return p;
            }
        }

        public IList<IPlugin> CustomInstanceCreators => m_customInstanceCreators;

        IEnumerable<IPlugin> IReadOnlyPluginCollection.DefaultInstanceCreators => DefaultInstanceCreators;

        IEnumerable<IPlugin> IReadOnlyPluginCollection.AfterNewInstanceCreated => AfterNewInstanceCreated;

        IEnumerable<IPlugin> IReadOnlyPluginCollection.LifecyclePlugins => LifecyclePlugins;

        IEnumerable<IPlugin> IReadOnlyPluginCollection.CustomInstanceCreators => CustomInstanceCreators;

        public IList<IPlugin> DefaultInstanceCreators => m_defaultInstanceCreators;

        public IList<IPlugin> AfterNewInstanceCreated => m_afterNewInstanceCreated;

        public IList<IPlugin> LifecyclePlugins => m_lifecyclePlugins;
    }
}
