using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class ErpRepository : IErpRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly ICache m_cache;

        public ErpRepository(IDatabase database, ISession session, ICache cache)
        {
            m_database = database;
            m_session = session;
            m_cache = cache;
        }

        public IErp GetErp(int id)
        {
            return GetAllErps().FirstOrDefault(e => e.Id == id);
        }

        public IEnumerable<IErp> GetAllErps()
        {
            var allErps = m_cache.ReadThrough<List<IErp>>(
                $"GetAllErpsBy_ProjectId:{m_session.Project.Id}",
                TimeSpan.FromHours(100),
                () =>
                {
                    return
                        m_database.SelectFrom<IErp>()
                            .Join(e => e.Project)
                            .Where(e => e.ProjectId == m_session.Project.Id)
                            .Execute()
                            .ToList();
                });

            return allErps;
        }
    }
}
