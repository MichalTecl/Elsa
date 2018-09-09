using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Elsa.Common.Caching
{
    public class Cache : ICache
    {
        private static readonly Random s_rnd = new Random();
        private static readonly ConcurrentDictionary<string, CacheEntry> s_cache = new ConcurrentDictionary<string, CacheEntry>();

        public T ReadThrough<T>(string key, TimeSpan timeout, Func<T> factory)
        {
            var entry = s_cache.GetOrAdd(key, k => new CacheEntry(DateTime.Now.Add(timeout), factory()));
            if (entry.ValidTo < DateTime.Now)
            {
                Remove(key);

                return ReadThrough(key, timeout, factory);
            }

            return (T)entry.Value;
        }

        public void Remove(string key)
        {
            CacheEntry dummy;
            s_cache.TryRemove(key, out dummy);
        }

        private sealed class CacheEntry
        {
            public readonly DateTime ValidTo;
            public readonly object Value;

            public CacheEntry(DateTime validTo, object value)
            {
                ValidTo = validTo;
                Value = value;
            }
        }

        private static void CacheCleanerLoop()
        {
            while (true)
            {
                Thread.Sleep(s_rnd.Next(500, 2000));

                var allKeys = s_cache.Keys.ToList();

                while (allKeys.Any())
                {
                    Thread.Sleep(s_rnd.Next(20, 200));

                    var itemIndex = s_rnd.Next(allKeys.Count);
                    var key = allKeys[itemIndex];
                    allKeys.RemoveAt(itemIndex);

                    CacheEntry entry;
                    if (!s_cache.TryGetValue(key, out entry))
                    {
                        continue;
                    }

                    if (entry.ValidTo < DateTime.Now)
                    {
                        s_cache.TryRemove(key, out entry);
                    }
                }
            }
        }

        static Cache()
        {
            var thr = new Thread(CacheCleanerLoop)
                          {
                              IsBackground = true,
                              Name = "CacheCleanerThread",
                              Priority = ThreadPriority.BelowNormal
                          };
            thr.Start();
        }
    }
}
