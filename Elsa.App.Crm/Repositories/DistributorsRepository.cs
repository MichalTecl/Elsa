using Elsa.App.Crm.Model;
using Elsa.Commerce.Core.Crm;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils.TextMatchers;
using Elsa.Core.Entities.Commerce.Crm;
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

        public List<DistributorGridRowModel> GetDistributors(DistributorGridFilter filter, int pageSize, int page, string sorterId)
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

                if (filter.Tags.Count > 0)
                {
                    foreach (var filterTagId in filter.Tags)
                        if (!d.TagTypeIds.Contains(filterTagId))
                            return false;
                }

                if (filter.CustomerGroupTypeId != null && !d.CustomerGroupTypeIds.Contains(filter.CustomerGroupTypeId.Value))
                    return false;

                if (filter.SalesRepresentativeId != null && !d.SalesRepIds.Contains(filter.SalesRepresentativeId.Value))
                    return false;

                if (!filter.IncludeDisabled && d.CustomerGroupTypeIds.Any(cgId => disabledGroupIds.Contains(cgId)))
                    return false;
                               
                
                return true;
            });

            var sorter = DistributorSorting.Sortings.FirstOrDefault(s => s.Id == sorterId) ?? DistributorSorting.Sortings.First();

            all = sorter.Sorter(all);

            all = all.Skip(pageSize * (page - 1)).Take(pageSize);

            var result = all.ToList();

            foreach(var a in result)
            {
                if (trends.TryGetValue(a.Id, out var trend))
                    a.TrendModel = trend;
                else
                    a.TrendModel = emptyTrend;
            }

            return result;
        }

        public DistributorDetailViewModel GetDetail(int customerId)
        {            
            return _database
                .Sql()
                .Call("LoadDistributorDetail")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@customerId", customerId)
                .AutoMap<DistributorDetailViewModel>()
                .FirstOrDefault();
        }

        public List<DistributorAddressViewModel> GetDistributorAddresses(int customerId)
        {
            return _cache.ReadThrough($"distributorAddresses_{customerId}", TimeSpan.FromMinutes(10), () => _database.Sql()
                .Call("LoadCustomerAddresses")
                .WithParam("@customerId", customerId)
                .AutoMap<DistributorAddressViewModel>());
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
                
        public class DistributorSorting
        {
            public static readonly DistributorSorting[] Sortings = new[]
            {
                new DistributorSorting("default", "Název A-Z", recs => recs.OrderBy(i => i.Name)),
                new DistributorSorting("idDesc", "Od nejnovějšího", recs => recs.OrderByDescending(i => i.Id)),
                new DistributorSorting("futureContactAsc", "Nejbližší plánovaný kontakt", recs => recs.Where(i => i.FutureContactDt != null).OrderBy(i => i.FutureContactDt)),
                new DistributorSorting("pastContactAsc", "Nejdéle od kontaktu", recs => recs.Where(i => i.LastContactDt != null).OrderBy(i => i.LastContactDt)),
                new DistributorSorting("pastContactDesc", "Nejčerstvější kontakt", recs => recs.Where(i => i.LastContactDt != null).OrderByDescending(i => i.LastContactDt)),
            };

            private DistributorSorting(string id, string text, Func<IEnumerable<DistributorGridRowModel>, IEnumerable<DistributorGridRowModel>> sorter)
            {
                Id = id;
                Text = text;
                Sorter = sorter;
            }

            public string Id { get; }
            public string Text { get; }
            public Func<IEnumerable<DistributorGridRowModel>, IEnumerable<DistributorGridRowModel>> Sorter { get; }
        }
    }
}
