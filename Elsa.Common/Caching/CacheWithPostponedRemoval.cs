using System;
using System.Collections.Generic;

namespace Elsa.Common.Caching
{
    public class CacheWithPostponedRemoval : ICache, IDisposable
    {
        private readonly HashSet<string> m_keysToRemove = new HashSet<string>();

        private readonly ICache m_underlyingCache;

        public CacheWithPostponedRemoval(ICache underlyingCache)
        {
            m_underlyingCache = underlyingCache;
        }

        public T ReadThrough<T>(string key, TimeSpan timeout, Func<T> factory)
        {
            return m_underlyingCache.ReadThrough(key, timeout, factory);
        }

        public void Remove(string key)
        {
            m_keysToRemove.Add(key);
        }

        public IEnumerable<string> GetAllKeys()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            foreach (var rek in m_keysToRemove)
            {
                m_underlyingCache.Remove(rek);
            }
        }
    }
}
