using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Inventory;

using RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ICache _cache;

        public UnitRepository(IDatabase database, ISession session, ICache cache)
        {
            _database = database;
            _session = session;
            _cache = cache;
        }

        public IMaterialUnit GetUnit(int unitId)
        {
            return GetAllUnits().FirstOrDefault(u => u.Id == unitId);
        }

        public IMaterialUnit GetUnitBySymbol(string symbol)
        {
            var units =
                GetAllUnits()
                    .Where(u => string.Equals(symbol, u.Symbol, StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();

            switch (units.Length)
            {
                case 0:
                    return null;
                case 1:
                    return units[0];
                default:
                    return units.FirstOrDefault(u => u.Symbol == symbol);
            }
        }

        public IEnumerable<IMaterialUnit> GetAllUnits()
        {
            return _cache.ReadThrough(
                $"AllMaterailUnitsBy_ProjectId:{_session.Project.Id}",
                TimeSpan.FromMinutes(10),
                () => _database.SelectFrom<IMaterialUnit>().Where(u => u.ProjectId == _session.Project.Id).Execute());
        }
    }
}
