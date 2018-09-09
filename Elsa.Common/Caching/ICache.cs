using System;

namespace Elsa.Common.Caching
{
    public interface ICache
    {
        T ReadThrough<T>(string key, TimeSpan timeout, Func<T> factory);

        void Remove(string key);
    }
}
