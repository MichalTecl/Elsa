using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Commerce.Core.Crm;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CrmMetadata")]
    public class CrmMetadataController : ElsaControllerBase
    {
        private readonly CustomerMeetingsRepository _customerMeetingsRepository;
        private readonly CustomerTagRepository _customerTagRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly SalesRepRepository _salesRepRepository;

        public CrmMetadataController(IWebSession webSession, ILog log, CustomerMeetingsRepository customerMeetingsRepository, CustomerTagRepository customerTagRepository, ICustomerRepository customerRepository, SalesRepRepository salesRepRepository) : base(webSession, log)
        {
            _customerMeetingsRepository = customerMeetingsRepository;
            _customerTagRepository = customerTagRepository;
            _customerRepository = customerRepository;
            _salesRepRepository = salesRepRepository;
        }

        public CrmMetadata Get()
        {
            return new CrmMetadata(
                _customerMeetingsRepository.GetMeetingStatusTypes(),
                _customerMeetingsRepository.GetMeetingStatusActions(null),
                _customerMeetingsRepository.GetAllMeetingCategories(),
                _customerTagRepository.GetTagTypes(false, false),
                _salesRepRepository.GetSalesRepresentatives(null).ToList(),
                _customerRepository.GetCustomerGroupTypes().Select(kv => kv.Value).ToList());
        }
    }
}
