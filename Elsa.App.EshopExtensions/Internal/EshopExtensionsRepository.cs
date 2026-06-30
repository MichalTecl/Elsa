using Elsa.Common.Interfaces;
using Robowire.RobOrm.Core;

namespace Elsa.App.EshopExtensions.Internal
{
    public class EshopExtensionsRepository : IEshopExtensionsRepository
    {
        private readonly IWebSession _session;
        private readonly IDatabase _database;

        public EshopExtensionsRepository(IWebSession session, IDatabase database)
        {
            _session = session;
            _database = database;
        }

        public EshopExtensionsStatus GetStatus()
        {
            return new EshopExtensionsStatus
            {
                ProjectId = _session.Project.Id,
                DatabaseConnected = _database != null
            };
        }
    }
}
