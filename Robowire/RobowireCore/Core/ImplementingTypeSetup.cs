using System;
using System.Collections.Generic;

using Robowire.Behavior;
using Robowire.Plugin;

namespace Robowire.Core
{
    internal class ImplementingTypeSetup<T> : SetupElementBase, IImplementingTypeSetup<T>, IInstanceImport<T>
    {
        private readonly InstanceRecord m_record;

        private IList<IPlugin> m_plugins;

        public ImplementingTypeSetup(InstanceRecord record, IList<IPlugin> plugins) : base(record, plugins)
        {
            m_record = record;
            m_plugins = plugins;
        }

        public IInstanceConstruction Use<TImpl>() where TImpl : T
        {
            return Use(typeof(TImpl));
        }

        public IInstanceImport<T> Import
        {
            get
            {
                return this;
            }
        }

        public IInstanceImport<T> FromFactory(Func<IServiceLocator, T> factoryFunction)
        {
            if (factoryFunction == null)
            {
                throw new ArgumentException(nameof(factoryFunction));
            }

            Func<IServiceLocator, object> objFactory = sl => factoryFunction(sl);

            FromFactory(objFactory);

            return this;
        }

        public IInstanceImport<T> Existing(T instance)
        {
            Existing((object)instance);

            return this;
        }

        public IInstanceConstruction Use(Type implType)
        {
            m_record.Factory = null;
            m_record.ImplementingType = implType;

            return new InstanceConstruction(m_record, m_plugins);
        }

        public IInstanceImport ImportObject
        {
            get
            {
                return this;
            }
        }

        public IInstanceImport FromFactory(Func<IServiceLocator, object> factoryFunction)
        {
            m_record.ConstructorParameters = null;
            m_record.ImplementingType = null;
            m_record.Factory = new NamedFactory(factoryFunction);

            return this;
        }

        public IInstanceImport Existing(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentException(nameof(instance));
            }

            Func<IServiceLocator, object> factory = sl => instance;

            m_record.ConstructorParameters = null;
            m_record.ImplementingType = instance.GetType();
            m_record.Factory = new NamedFactory(factory);
            m_record.AddBehavior(new DisposeBehavior() { Dispose = false });

            return this;
        }

        internal ImplementingTypeSetup<T2> ChangeType<T2>()
        {
            return new ImplementingTypeSetup<T2>(m_record, m_plugins);
        }

    }
}
