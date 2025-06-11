using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Common.Logging;
using Elsa.Jobs.Common;
using Elsa.Jobs.SyncErpCustomers.Mailchimp;

namespace Elsa.Jobs.SyncErpCustomers
{
    public class CustomersSyncJob : IExecutableJob
    {
        private readonly IErpClientFactory m_erpClientFactory;
        private readonly ICustomerRepository m_customerRepository;
        private readonly ILog m_log;

        public CustomersSyncJob(IErpClientFactory erpClientFactory, ICustomerRepository customerRepository, ILog log)
        {
            m_erpClientFactory = erpClientFactory;
            m_customerRepository = customerRepository;
            m_log = log;
        }

        public void Run(string customDataJson)
        {
            //m_customerRepository.SyncShadowCustomers();

            m_log.Info("Starting synchronization of customers from ERP");
            foreach (var erp in m_erpClientFactory.GetAllErpClients())
            {
                var erpCustomerModels = erp.LoadCustomers().ToList();
                if (!erpCustomerModels.Any())
                {
                    continue;
                }

                m_customerRepository.SyncCustomers(erpCustomerModels);
            }            
        }
    }
}
