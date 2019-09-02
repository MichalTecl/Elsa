using System;
using System.Collections.Concurrent;
using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Common.Data
{
    public abstract class AdapterBase<T> : IAdapter<T>
    {
        private readonly IServiceLocator m_serviceLocator;
        private readonly ConcurrentDictionary<string, object> m_lazies = new ConcurrentDictionary<string, object>();

        protected AdapterBase(IServiceLocator serviceLocator, T adaptee)
        {
            m_serviceLocator = serviceLocator;
            Adaptee = adaptee;
        }

        protected T Get<TRepo, T>(string propertyName, Func<TRepo, T> factory) where T:class
        {
            return m_lazies.GetOrAdd(propertyName, pn =>
            {
                var repository = m_serviceLocator.Get<TRepo>();
                return factory(repository);
            }) as T;
        }

        public T Adaptee { get; }
    }
}
