using Elsa.App.Crm.Entities;
using Elsa.App.Crm.Model;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Crm;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories
{
    public class SalesRepRepository
    {
        private readonly IDatabase _db;
        private readonly ISession _session;

        public SalesRepRepository(IDatabase db, ISession session)
        {
            _db = db;
            _session = session;
        }

        public IEnumerable<ISalesRepresentative> GetSalesRepresentatives(string searchText) 
        {
            return _db.SelectFrom<ISalesRepresentative>().Where(s => s.ProjectId == _session.Project.Id).Execute();
        }

        public IEnumerable<ICustomer> GetDistributors(int? salesRepId) 
        {
            if (salesRepId != null) 
            {
                return _db.SelectFrom<ISalesRepCustomer>()
                    .Join(sc => sc.Customer)
                    .Where(src => src.SalesRepId == salesRepId)
                    .Execute()
                    .Select(r => r.Customer)
                    .GroupBy(x => x.Id)
                    .Select(g => g.First());
            }

            return _db.SelectFrom<ICustomer>()
                .Where(c => c.ProjectId == _session.Project.Id)
                .Where(c => c.IsDistributor)
                .Execute();
        }

        public IEnumerable<ISalesRepCustomer> GetSrCustomers() 
        {
            return _db.SelectFrom<ISalesRepCustomer>()
                .Join(s => s.SalesRep)
                .Where(s => s.SalesRep.ProjectId == _session.Project.Id)
                .Execute()
                .Where(i => i.ValidFrom <= DateTime.Now && i.ValidTo == null || i.ValidTo >= DateTime.Now)
                .ToList();
        }
    }
}
