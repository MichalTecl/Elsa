using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Jobs.Common;

namespace Elsa.Jobs.SyncErpCustomers
{
    public class CustomersSyncJob : IExecutableJob
    {
        private readonly IErpClientFactory m_erpClientFactory;
        private readonly ICustomerRepository m_customerRepository;

        public CustomersSyncJob(IErpClientFactory erpClientFactory, ICustomerRepository customerRepository)
        {
            m_erpClientFactory = erpClientFactory;
            m_customerRepository = customerRepository;
        }

        public void Run(string customDataJson)
        {
            m_customerRepository.SyncShadowCustomers();

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
