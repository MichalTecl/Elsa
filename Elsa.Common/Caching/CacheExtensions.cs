using System.Collections.Generic;

namespace Elsa.Common.Caching
{
    public static class CacheExtensions
    {
        public static void Remove(this ICache cache, IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                cache.Remove(key);
            }
        }
    }
}
