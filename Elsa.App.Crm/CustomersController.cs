using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.App.Crm.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Common;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.App.Crm
{
    [Controller("customers")]
    public class CustomersController : ElsaControllerBase
    {
        private readonly ICustomerRepository m_customerRepository;
        private readonly IUserRepository m_userRepository;

        public CustomersController(IWebSession webSession, ILog log, ICustomerRepository customerRepository, IUserRepository userRepository)
            : base(webSession, log)
        {
            m_customerRepository = customerRepository;
            m_userRepository = userRepository;
        }

        public CustomerViewModel GetCustomer(string email)
        {
            var customer = m_customerRepository.GetOverview(email);
            if (customer == null)
            {
                return null;
            }

            return new CustomerViewModel(customer, WebSession.Project, m_userRepository);
        }

    }
}
