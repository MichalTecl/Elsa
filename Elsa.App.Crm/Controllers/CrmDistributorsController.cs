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

        public List<DistributorGridRowModel> GetDistributors(DistributorGridFilter filter, int pageSize, int page, string sortBy, bool ascending)
        {
            return _distributorsRepository.GetDistributors(filter, pageSize, page, sortBy, ascending);
        }
    }
}
