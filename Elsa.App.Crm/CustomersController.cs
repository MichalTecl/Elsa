using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.App.Crm.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.App.Crm
{
    [Controller("customers")]
    public class CustomersController : ElsaControllerBase
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;

        public CustomersController(IWebSession webSession, ILog log, ICustomerRepository customerRepository, IUserRepository userRepository)
            : base(webSession, log)
        {
            _customerRepository = customerRepository;
            _userRepository = userRepository;
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
    }
}
