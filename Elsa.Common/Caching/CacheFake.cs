using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Caching
{
    public sealed class CacheFake : ICache
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
            
        }

        public IEnumerable<string> GetAllKeys()
        {
            yield break;
        }

        public void Clear()
        {
        }
    }
}
