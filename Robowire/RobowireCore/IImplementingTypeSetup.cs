using System;

namespace Robowire
{
    public interface IImplementingTypeSetup<T> : IImplementingTypeSetup
    {
        IInstanceConstruction Use<TImpl>() where TImpl : T;

        IInstanceImport<T> Import { get; }
    }

    public interface IImplementingTypeSetup : IWithBehavior
    {
        IInstanceConstruction Use(Type implType);

        IInstanceImport ImportObject { get; }
    }

}
