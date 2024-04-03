using System;
using System.Collections.Generic;
using System.Reflection;

namespace Robowire
{
    public interface IServiceLocator : IDisposable
    {
        IServiceLocator Parent { get; }

        T Get<T>();

        object Get(Type t);

        IEnumerable<T> GetCollection<T>();

        object InstantiateNow(ConstructorInfo ctor);

        object InstantiateNow(Type type);

        T InstantiateNow<T>();

        T InstantiateNow<T>(string assemblyQualifiedTypeName);

        T InstantiateNow<T>(Type type) where T : class;
    }
}
