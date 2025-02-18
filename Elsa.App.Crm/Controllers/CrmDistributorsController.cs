using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
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

        public CrmDistributorsController(IWebSession webSession, ILog log, DistributorsRepository distributorsRepository) : base(webSession, log)
        {
            _distributorsRepository = distributorsRepository;
        }

        public List<DistributorGridRowModel> GetDistributors(DistributorGridFilter filter, int pageSize, int page, string sorterId)
        {
            return _distributorsRepository.GetDistributors(filter, pageSize, page, sorterId);
        }

        public DistributorSorting[] GetSortingTypes() => DistributorSorting.Sortings;
    }
}
