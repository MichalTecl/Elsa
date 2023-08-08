using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Users.SyncJob
{
    public class SyncUserRightTypesStartupJob : IStartupJob
    {
        private readonly ILog _log;
        private readonly IUserRoleRepository _roleRepo;

        public SyncUserRightTypesStartupJob(ILog log, IUserRoleRepository roleRepo)
        {
            _log = log;
            _roleRepo = roleRepo;
        }

        public bool IsExceptionFatal => true;

        public void Execute()
        {
            var allUserRights = _roleRepo.GetAllUserRights();
            _log.Info($"Synced UserRights: {allUserRights}");
        }
    }
}
