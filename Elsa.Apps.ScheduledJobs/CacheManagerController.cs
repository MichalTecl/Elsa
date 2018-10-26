using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.Apps.ScheduledJobs
{
    [Controller("cacheManager")]
    public class CacheManagerController : ElsaControllerBase
    {
        private readonly ICache m_cache;

        public CacheManagerController(IWebSession webSession, ILog log, ICache cache)
            : base(webSession, log)
        {
            m_cache = cache;
        }

        public IEnumerable<string> List()
        {
            return m_cache.GetAllKeys();
        }

        public void Clear()
        {
            m_cache.Clear();
        }
    }
}
