using System;
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
    }
}
