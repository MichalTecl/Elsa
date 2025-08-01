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
        private readonly IErpClientFactory _erpClientFactory;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILog _log;

        public CustomersSyncJob(IErpClientFactory erpClientFactory, ICustomerRepository customerRepository, ILog log)
        {
            _erpClientFactory = erpClientFactory;
            _customerRepository = customerRepository;
            _log = log;
        }

        private static readonly Mutex _mutex = new Mutex();

        public void Run(string customDataJson)
        {
            bool acquired = false;

            try
            {
                acquired = _mutex.WaitOne(TimeSpan.FromSeconds(5));
                if (!acquired)
                {
                    throw new InvalidOperationException("Právě probíhá import zákazníků, opakujte akci později.");
                }

                _log.Info("Starting synchronization of customers from ERP");
                foreach (var erp in _erpClientFactory.GetAllErpClients())
                {
                    var erpCustomerModels = erp.LoadCustomers().ToList();
                    if (!erpCustomerModels.Any())
                    {
                        continue;
                    }

                    _customerRepository.SyncCustomers(erpCustomerModels);
                }
            }
            finally
            {
                if (acquired)
                {
                    _mutex.ReleaseMutex();
                }
            }
        }

    }
}