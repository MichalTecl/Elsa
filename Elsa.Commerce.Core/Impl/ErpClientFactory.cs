﻿using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Impl
{
    public class ErpClientFactory : IErpClientFactory
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly IServiceLocator m_serviceLocator;
        private readonly IErpRepository m_erpRepository;
        
        public ErpClientFactory(IDatabase database, ISession session, IServiceLocator serviceLocator, IErpRepository erpRepository)
        {
            m_database = database;
            m_session = session;
            m_serviceLocator = serviceLocator;
            m_erpRepository = erpRepository;
        }

        public IErpClient GetErpClient(int erpId)
        {
            var erp =
                m_database.SelectFrom<IErp>()
                    .Where(i => (i.ProjectId == m_session.Project.Id) && (i.Id == erpId))
                    .Execute()
                    .FirstOrDefault();

            if (erp == null)
            {
                throw new InvalidOperationException("ErpClient not found");
            }

            var erpClient = m_serviceLocator.InstantiateNow<IErpClient>(erp.ClientClass);
            erpClient.Erp = erp;

            return erpClient;
        }

        public IEnumerable<IErpClient> GetAllErpClients()
        {
            return m_erpRepository.GetAllErps().Select(e => GetErpClient(e.Id));
        }
    }
}
