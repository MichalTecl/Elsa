using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Robowire.Plugin;
using Robowire.Plugin.DefaultPlugins;
using Robowire.Plugin.Flow;

namespace Robowire.Core
{
    internal class ContainerSetup : IContainerSetup
    {
        private readonly List<InstanceRecord> m_instanceRecords;

        private readonly IPluginCollection m_plugins;

        private readonly List<IGeneratedCodeListener> m_generatorListeners;

        public ContainerSetup(List<InstanceRecord> instanceRecords, IPluginCollection plugins, List<IGeneratedCodeListener> generatorListeners)
        {
            m_instanceRecords = instanceRecords;
            m_plugins = plugins;
            m_generatorListeners = generatorListeners;
        }

        public IImplementingTypeSetup<T> For<T>()
        {
            return ((ImplementingTypeSetup<object>)For(typeof(T))).ChangeType<T>();
        }

        public IImplementingTypeSetup For(Type interfaceType)
        {
            var oldRecord = m_instanceRecords.FirstOrDefault(r => r.InterfaceType == interfaceType);
            if (oldRecord != null)
            {
                m_instanceRecords.Remove(oldRecord);
            }

            var record = new InstanceRecord()
            {
                InterfaceType = interfaceType
            };

            m_instanceRecords.Add(record);

            return new ImplementingTypeSetup<object>(record, m_plugins.AllPlugins.ToList());
        }

        public IContainerSetup RegisterPlugin(Action<IPluginCollection> setup)
        {
            setup(m_plugins);

            return this;
        }

        public IContainerSetup Collect<T>()
        {
            return Collect(typeof(T));
        }

        public IContainerSetup Collect(Type baseType)
        {
            if (m_plugins.AllPlugins.OfType<CollectorPlugin>().Any(i => i.CollectionType == baseType))
            {
                return this;
            }

            return RegisterPlugin(s => s.AfterNewInstanceCreated.Add(new CollectorPlugin(baseType)));
        }

        public IContainerSetup SubscribeCodeGeneratorListener(IGeneratedCodeListener listener)
        {
            if (!m_generatorListeners.Contains(listener))
            {
                m_generatorListeners.Add(listener);
            }

            return this;
        }

        public IContainerSetup ScanAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                ScanType(type);
            }

            return this;
        }

        public IContainerSetup ScanType(Type type)
        {
            if (typeof(IRobowireRegistry).IsAssignableFrom(type) && (!type.IsInterface) && (!type.IsAbstract))
            {
                var inst = Activator.CreateInstance(type) as IRobowireRegistry;
                inst?.Setup(this);
            }

            var attributes = type.GetCustomAttributes().OfType<ISelfSetupAttribute>();

            foreach (var attribute in attributes)
            {
                attribute.Setup(type, this);
            }

            return this;
        }

        public IContainerSetup ScanType<T>()
        {
            return ScanType(typeof(T));
        }

        public IEnumerable<T> GetRegisteredPlugins<T>() where T : IPlugin
        {
            return m_plugins.AllPlugins.OfType<T>();
        }
    }
}
