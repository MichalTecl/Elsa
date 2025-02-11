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

            var trends = GetTrendIndex();
            var emptyTrend = new List<SalesTrendTick>(0);

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

                // just reusing the filtering loop...

                if (trends.TryGetValue(d.Id, out var trend))
                    d.TrendModel = trend;
                else
                    d.TrendModel = emptyTrend;

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

        private Dictionary<int, List<SalesTrendTick>> GetTrendIndex()
        {
            return _cache.ReadThrough($"distributorOrdersHistoryTrend_{_session.Project.Id}", TimeSpan.FromHours(1), () =>
            {
                var trends = new Dictionary<int, SalesTrend>();

                const int historyDepth = 23;

                _database.Sql()
                    .Call("getDistributorsSales")
                    .WithParam("@projectId", _session.Project.Id)
                    .WithParam("@historyDepth", historyDepth)
                    .ReadRows<int, int, decimal>((customerId, month, value) =>
                    {

                        if (!trends.TryGetValue(customerId, out var trend))
                            trends[customerId] = trend = new SalesTrend(historyDepth);

                        trend.Add(month, value);
                    });

                return trends.ToDictionary(t => t.Key, t => t.Value.GetModel().ToList());
            });
        }

        private static object GetSorterValue(object i, string prop)
        {
            return i.GetType().GetProperty(prop).GetValue(i, null);
        }
    }
}
