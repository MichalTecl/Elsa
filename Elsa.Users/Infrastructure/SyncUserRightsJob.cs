using Elsa.Common.Interfaces;
using Robowire.RobOrm.Core;

namespace Elsa.Users.Infrastructure
{
    public class SyncUserRightsJob : IStartupJob
    {
        private readonly IDatabase m_database;

        public SyncUserRightsJob(IDatabase database)
        {
            m_database = database;
        }

        public bool IsExceptionFatal { get; } = true;

        public void Execute()
        {
            var codeRights = UserRightDefinitionCollector.GetAllUserRights();

        }
    }
}
