using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Integration;

using RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class ErpRepository : IErpRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ICache _cache;

        public ErpRepository(IDatabase database, ISession session, ICache cache)
        {
            _database = database;
            _session = session;
            _cache = cache;
        }

        public IErp GetErp(int id)
        {
            return GetAllErps().FirstOrDefault(e => e.Id == id);
        }

        public IEnumerable<IErp> GetAllErps()
        {
            var allErps = _cache.ReadThrough<List<IErp>>(
                $"GetAllErpsBy_ProjectId:{_session.Project.Id}",
                TimeSpan.FromHours(100),
                () =>
                {
                    return
                        _database.SelectFrom<IErp>()
                            .Join(e => e.Project)
                            .Where(e => e.ProjectId == _session.Project.Id)
                            .Execute()
                            .ToList();
                });

            return allErps;
        }
    }
}
