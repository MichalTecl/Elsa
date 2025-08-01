using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.App.Crm.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Jobs.SyncErpCustomers;
using Robowire.RoboApi;

namespace Elsa.App.Crm
{
    [Controller("customers")]
    public class CustomersController : ElsaControllerBase
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly CustomersSyncJob _customersSyncJob;
        private readonly ICache _cache;

        public CustomersController(IWebSession webSession, ILog log, ICustomerRepository customerRepository, IUserRepository userRepository, CustomersSyncJob customersSyncJob, ICache cache)
            : base(webSession, log)
        {
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _customersSyncJob = customersSyncJob;
            _cache = cache;
        }

        public CustomerViewModel GetCustomer(string email)
        {            
            var customer = _customerRepository.GetOverview(email);
            if (customer == null)
            {
                return null;
            }

            return new CustomerViewModel(customer, WebSession.Project, _userRepository);
        }

        public IEnumerable<CustomerViewModel> GetCustomers(List<string> emails)
        {            
            return _customerRepository.GetOverviews(emails).Select(o => new CustomerViewModel(o, WebSession.Project, _userRepository));
        }

        public void SnoozeCustomer(int customerId)
        {
            _customerRepository.SnoozeCustomer(customerId);
        }

        public void ImportCustomers()
        {
            _customersSyncJob.Run(null);
            _cache.Clear();
        }
    }
}
