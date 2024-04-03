using System;

namespace Robowire
{
    public interface IWithBehavior
    {
        /// <summary>
        /// example: container.Setup(s => s.For<IInterface>().Use<Class>().WithBehavior<LifecycleSetup>(lc => lc.IsSingleton = false));
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setup"></param>
        /// <returns></returns>
        IWithBehavior WithBehavior<T>(Action<T> setup) where T : IBehavior, new();
    }
}
