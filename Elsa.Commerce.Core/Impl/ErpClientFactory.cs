using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire;
using RobOrm.Core;

namespace Elsa.Commerce.Core.Impl
{
    public class ErpClientFactory : IErpClientFactory
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly IServiceLocator _serviceLocator;
        private readonly IErpRepository _erpRepository;
        
        public ErpClientFactory(IDatabase database, ISession session, IServiceLocator serviceLocator, IErpRepository erpRepository)
        {
            _database = database;
            _session = session;
            _serviceLocator = serviceLocator;
            _erpRepository = erpRepository;
        }

        public IErpClient GetErpClient(int erpId)
        {
            var erp =
                _database.SelectFrom<IErp>()
                    .Where(i => (i.ProjectId == _session.Project.Id) && (i.Id == erpId))
                    .Execute()
                    .FirstOrDefault();

            if (erp == null)
            {
                throw new InvalidOperationException("ErpClient not found");
            }

            var erpClient = _serviceLocator.InstantiateNow<IErpClient>(erp.ClientClass);
            erpClient.Erp = erp;

            return erpClient;
        }

        public IEnumerable<IErpClient> GetAllErpClients()
        {
            return _erpRepository.GetAllErps().Select(e => GetErpClient(e.Id));
        }
    }
}
