using System;
using System.Collections.Generic;

namespace Elsa.Common.Caching
{
    public interface ICache
    {
        T ReadThrough<T>(string key, TimeSpan timeout, Func<T> factory);

        void Remove(string key);

        void RemoveByPrefix(string prefix);

        IEnumerable<string> GetAllKeys();

        void Clear();
    }
}
