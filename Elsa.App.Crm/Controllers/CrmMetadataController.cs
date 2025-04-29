using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Commerce.Core.Crm;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
        private readonly DistributorFiltersRepository _distributorFiltersRepository;

        public CrmMetadataController(IWebSession webSession, ILog log, CustomerMeetingsRepository customerMeetingsRepository, CustomerTagRepository customerTagRepository, ICustomerRepository customerRepository, SalesRepRepository salesRepRepository, DistributorFiltersRepository distributorFiltersRepository) : base(webSession, log)
        {
            _customerMeetingsRepository = customerMeetingsRepository;
            _customerTagRepository = customerTagRepository;
            _customerRepository = customerRepository;
            _salesRepRepository = salesRepRepository;
            _distributorFiltersRepository = distributorFiltersRepository;
        }

        public CrmMetadata Get()
        {
            return new CrmMetadata(
                _customerMeetingsRepository.GetMeetingStatusTypes(),
                _customerMeetingsRepository.GetMeetingStatusActions(null),
                _customerMeetingsRepository.GetAllMeetingCategories(),
                _customerTagRepository.GetTagTypes(false, false),
                _salesRepRepository.GetSalesRepresentatives(null).ToList(),
                _customerRepository.GetCustomerGroupTypes().Select(kv => kv.Value).ToList(),
                _distributorFiltersRepository.GetFilters());
        }

        public void SaveTag(int? id, string name, string cssClass)
        {
            if (id == null)
            {
                _customerTagRepository.CreateTagType(name, name, 1, false, cssClass);
            }
            else
            {
                _customerTagRepository.UpdateTagType(id.Value, name, name, 1, false, cssClass);
            }
        }

        public void DeleteTag(int id) 
        {
            _customerTagRepository.DeleteTagType(id);
        }

        public int CountTagAssignments(int id)
        {
            return _customerTagRepository.GetAssignmentsCount(id);
        }
    }
}
