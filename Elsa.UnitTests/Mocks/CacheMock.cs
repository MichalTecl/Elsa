using System;
using System.Collections.Generic;

using Elsa.Common.Caching;

namespace Elsa.UnitTests.Mocks
{
    public class CacheMock : ICache
    {
        public T ReadThrough<T>(string key, TimeSpan timeout, Func<T> factory)
        {
            return factory();
        }

        public void Remove(string key)
        {
        }

        public void RemoveByPrefix(string prefix)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetAllKeys()
        {
            throw new NotImplementedException();
        }

        public bool KeyExists(string key)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }
    }
}
