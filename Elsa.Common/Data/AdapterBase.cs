using System;
using System.Collections.Concurrent;
using Robowire;
using RobOrm.Core;

namespace Elsa.Common.Data
{
    public abstract class AdapterBase<T> : IAdapter<T>
    {
        private readonly IServiceLocator _serviceLocator;
        private readonly ConcurrentDictionary<string, object> _lazies = new ConcurrentDictionary<string, object>();

        protected AdapterBase(IServiceLocator serviceLocator, T adaptee)
        {
            _serviceLocator = serviceLocator;
            Adaptee = adaptee;
        }

        protected T Get<TRepo, T>(string propertyName, Func<TRepo, T> factory) where T:class
        {
            return _lazies.GetOrAdd(propertyName, pn =>
            {
                var repository = _serviceLocator.Get<TRepo>();
                return factory(repository);
            }) as T;
        }

        public T Adaptee { get; }
    }
}
