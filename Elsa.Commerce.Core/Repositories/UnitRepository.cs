using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Inventory;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly ICache m_cache;

        public UnitRepository(IDatabase database, ISession session, ICache cache)
        {
            m_database = database;
            m_session = session;
            m_cache = cache;
        }

        public IMaterialUnit GetUnit(int unitId)
        {
            return GetAllUnits().FirstOrDefault(u => u.Id == unitId);
        }

        public IEnumerable<IMaterialUnit> GetAllUnits()
        {
            return m_cache.ReadThrough(
                $"AllMaterailUnitsBy_ProjectId:{m_session.Project.Id}",
                TimeSpan.FromMinutes(10),
                () => m_database.SelectFrom<IMaterialUnit>().Where(u => u.ProjectId == m_session.Project.Id).Execute());
        }
    }
}
