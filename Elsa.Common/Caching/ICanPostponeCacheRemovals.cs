using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Caching
{
    public interface ICanPostponeCacheRemovals<T> where T : IDisposable
    {
        T GetWithPostponedCache();
    }
}
