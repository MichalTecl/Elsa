using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Elsa.App.Crm.Repositories.DistributorsRepository;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CrmDistributors")]
    public class CrmDistributorsController : ElsaControllerBase
    {
        private readonly DistributorsRepository _distributorsRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepo;

        public CrmDistributorsController(IWebSession webSession, ILog log, DistributorsRepository distributorsRepository, ICustomerRepository customerRepository, IUserRepository userRepo) : base(webSession, log)
        {
            _distributorsRepository = distributorsRepository;
            _customerRepository = customerRepository;
            _userRepo = userRepo;
        }

        public List<DistributorGridRowModel> GetDistributors(DistributorGridFilter filter, int pageSize, int page, string sorterId)
        {
            return _distributorsRepository.GetDistributors(filter, pageSize, page, sorterId);
        }

        public DistributorSorting[] GetSortingTypes() => DistributorSorting.Sortings;

        public DistributorDetailViewModel GetDetail(int customerId) => _distributorsRepository.GetDetail(customerId);

        public List<DistributorAddressViewModel> GetAddresses(int customerId) => _distributorsRepository.GetDistributorAddresses(customerId);

        public IEnumerable<CustomerNoteViewModel> GetNotes(int customerId)
        {
            var usindex = _userRepo.GetUserIndex();

            return _customerRepository.GetCustomerRelatedNotes(customerId)
                .Select(n => new CustomerNoteViewModel 
                {
                    Author = usindex.Get(n.CustomerId, null)?.EMail,
                    NoteDt = StringUtil.FormatDateTime(n.CreateDt),
                    Text = n.Body
                });
        }

        protected override void OnBeforeCall()
        {
            EnsureUserRight(CrmUserRights.DistributorsApp);
            base.OnBeforeCall();
        }
    }
}
