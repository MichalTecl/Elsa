using System;

namespace Robowire
{
    public interface IInstanceImport<T> : IInstanceImport
    {
        IInstanceImport<T> FromFactory(Func<IServiceLocator, T> factoryFunction);

        IInstanceImport<T> Existing(T instance);
    }

    public interface IInstanceImport : IWithBehavior
    {
        IInstanceImport FromFactory(Func<IServiceLocator, object> factoryFunction);

        IInstanceImport Existing(object instance);
    }
}
