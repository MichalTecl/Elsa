using Elsa.App.Crm.Model;
using Elsa.Commerce.Core.Crm;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils.TextMatchers;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.App.Crm.Repositories
{
    public class DistributorsRepository
    {
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly ISession _session;
        private readonly ICustomerRepository _customerRepository;

        public DistributorsRepository(IDatabase database, ICache cache, ISession session, ICustomerRepository customerRepository)
        {
            _database = database;
            _cache = cache;
            _session = session;
            _customerRepository = customerRepository;
        }

        internal List<DistributorGridRowModel> GetDistributors(DistributorGridFilter filter, int pageSize, int page, string sortProperty, bool ascending)
        {
            var disabledGroupIds = _cache
                .ReadThrough(
                    $"disabledCustomerGroupTypes_{_session.Project.Id}", 
                    TimeSpan.FromMinutes(10), 
                    () => new HashSet<int>(_customerRepository
                            .GetCustomerGroupTypes()
                            .Values
                            .Where(cg => cg.IsDisabled)
                            .Select(cg => cg.Id)
                            ));

            var matcher = SearchTagMatcher.GetMatcher(filter.TextFilter);

            var all = GetAllDistributors().Where(d =>
            {
                if (!matcher.Match(d.SearchTag))
                    return false;

                if (filter.Tags.Any())
                    if (!filter.Tags.All(filterTagId => d.TagTypeIds.Contains(filterTagId)))
                        return false;

                if (filter.CustomerGroupTypeId != null && !d.CustomerGroupTypeIds.Contains(filter.CustomerGroupTypeId.Value))
                    return false;

                if (filter.SalesRepresentativeId != null && !d.SalesRepIds.Contains(filter.SalesRepresentativeId.Value))
                    return false;

                if (!filter.IncludeDisabled && d.CustomerGroupTypeIds.Any(cgId => disabledGroupIds.Contains(cgId)))
                    return false;
                
                return true;
            });

            if (!string.IsNullOrEmpty(sortProperty))
            {
                all = ascending ? all.OrderBy(i => GetSorterValue(i, sortProperty)) : all.OrderByDescending(i => GetSorterValue(i, sortProperty));
            }

            all = all.Skip(pageSize * page).Take(pageSize);

            return all.ToList();
        }

        private List<DistributorGridRowModel> GetAllDistributors()
        {
            return _database.Sql().Call("LoadAllDistributors")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@userId", _session.User.Id)
                .AutoMap<DistributorGridRowModel>();
        }

        private static object GetSorterValue(object i, string prop)
        {
            return i.GetType().GetProperty(prop).GetValue(i, null);
        }
    }
}
