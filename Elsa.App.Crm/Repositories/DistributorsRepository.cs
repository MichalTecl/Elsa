using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure;
using Elsa.Commerce.Core.Crm;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Common.Utils.TextMatchers;
using Elsa.Core.Entities.Commerce.Crm;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace Elsa.App.Crm.Repositories
{
    public class DistributorsRepository
    {
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly ISession _session;
        private readonly ICustomerRepository _customerRepository;
        private readonly DistributorFiltersRepository _distributorFilters;
        private readonly ILog _log;
        private readonly Lazy<ColumnFactory> _columnFactory;

        public DistributorsRepository(IDatabase database, ICache cache, ISession session, ICustomerRepository customerRepository, DistributorFiltersRepository distributorFilters, ILog log, Lazy<ColumnFactory> columnFactory)
        {
            _database = database;
            _cache = cache;
            _session = session;
            _customerRepository = customerRepository;
            _distributorFilters = distributorFilters;
            _log = log;
            _columnFactory = columnFactory;
        }

        public List<DistributorGridRowModel> GetDistributors(DistributorGridFilter filter, int? pageSize, int? page, bool idsOnly = false)
        {
            _log.Info($"Received distributors query");

            var idsByDistributorFilters = GetIdIndexByDistributorFilters(filter);

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

            var trends = idsOnly ? null : GetTrendIndex();
            var emptyTrend = new List<SalesTrendTick>(0);

            var matcher = SearchTagMatcher.GetMatcher(filter.TextFilter);

            var all = GetAllDistributors().Where(d =>
            {
                if (idsByDistributorFilters?.Contains(d.Id) == false)
                    return false;

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
            })
                .ToList();
                        
            List<DynamicColumnWrapper> dynColumns = null;
            if (!idsOnly)
            {
                dynColumns = _columnFactory.Value.GetColumns(filter.GridColumns.Where(c => c.IsSelected).Select(c => c.Id).ToArray());

                if (!string.IsNullOrWhiteSpace(filter.SortBy))
                {
                    var sortCol = dynColumns.FirstOrDefault(c => c.Column.Id == filter.SortBy);
                    if (sortCol == null)
                    {
                        _log.Error($"Invalid SortBy '{filter.SortBy}'");
                        filter.SortBy = null;
                    }

                    sortCol.Populate(all, filter.SortDescending);
                }
            }

            if (pageSize != null && page != null)
            {
                all = all.Skip(pageSize.Value * (page.Value - 1)).Take(pageSize.Value).ToList();
            }

            var result = all.ToList();

            

            if (!idsOnly)
            {
                var columns = _columnFactory.Value.GetColumns(filter.GridColumns.Where(c => c.IsSelected && (c.Id != filter.SortBy)).Select(c => c.Id).ToArray());
                
                foreach (var c in columns)
                {
                    c.Populate(result, null);
                }

                foreach (var a in result)
                {
                    if (trends.TryGetValue(a.Id, out var trend))
                        a.TrendModel = trend;
                    else
                        a.TrendModel = emptyTrend;
                }
            }

            return result;
        }

        private HashSet<int> GetIdIndexByDistributorFilters(DistributorGridFilter filter)
        {
            if (filter.ExFilterGroups == null || filter.ExFilterGroups.Count == 0)
                return null;

            _log.Info($"Distributors query has {filter.ExFilterGroups.Count} distributor filter groups");

            List<HashSet<int>> idGroups = new List<HashSet<int>>(filter.ExFilterGroups.Count);

            foreach (var orGroup in filter.ExFilterGroups)
            {
                _log.Info($"Processing group:");

                var idGroup = new HashSet<int>();
                idGroups.Add(idGroup);

                foreach (var dFilter in orGroup.Filters)
                {
                    _log.Info($"Executing filter {dFilter.Title}");

                    try
                    {
                        var fResult = _distributorFilters.Execute(dFilter);
                        _log.Info($"Execution result: RecordsCount={fResult.Ids.Count}");
                        idGroup.AddRange(fResult.Ids);
                        _log.Info($"Group has {idGroup.Count} of ids");
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Filter execution failed", ex);
                        throw;
                    }
                }
            }

            _log.Info($"We have {idGroups.Count} of id groups, going to find ids included in all of them");

            if (idGroups.Count == 1)
                return idGroups[0];


            idGroups = idGroups.OrderBy(g => g.Count).ToList();

            var idsInAllExFilters = new HashSet<int>();

            var firstGroup = idGroups.FirstOrDefault() ?? new HashSet<int>(0);

            foreach (var id in firstGroup)
            {
                var found = true;
                foreach (var idGroup in idGroups.Skip(1))
                {
                    if (!idGroup.Contains(id))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                    idsInAllExFilters.Add(id);
            }

            _log.Info($"Done. We got {idsInAllExFilters.Count} of ids from distributor fitlers");
            return idsInAllExFilters;
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
            return _database.Sql()
                .Call("LoadCustomerAddresses")
                .WithParam("@customerId", customerId)
                .AutoMap<DistributorAddressViewModel>();
        }

        public List<ICustomerStore> GetStores(int customerId)
        {
            return _database
                .SelectFrom<ICustomerStore>()
                .Join(s => s.Customer)
                .Where(s => s.Customer.ProjectId == _session.Project.Id)
                .Where(s => s.CustomerId == customerId).Execute().ToList();
        }

        public void DeleteStore(int customerId, string addressName)
        {
            var store = GetStores(customerId).FirstOrDefault(s => s.SystemRecordName == addressName).Ensure("Invalid address name");

            _database.Delete(store);
        }

        public void SaveStore(int customerId, string addressName, Action<ICustomerStore> change)
        {
            var store = GetStores(customerId)
                .FirstOrDefault(s => s.SystemRecordName == addressName)
                ?? _database.New<ICustomerStore>(s =>
                {
                    s.SystemRecordName = addressName;
                    s.CustomerId = customerId;
                });

            change(store);

            _database.Save(store);
        }

        public List<DistributorGridRowModel> GetAllDistributors()
        {
            return _database.Sql().Call("LoadAllDistributors")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@userId", _session.User.Id)
                .AutoMap<DistributorGridRowModel>();
        }

        public IReadOnlyCollection<CustomerHistoryEntryModel> GetCustomerHistory(int customerId, bool lastOnly, bool timeDesc)
        {
            var sql = "SELECT * FROM vwCustomerEvents e WHERE e.CustomerId={0}";

            if (lastOnly)
                sql += " AND e.IsLastEvent=1";
            else
            {
                sql += " ORDER BY e.EventDt";

                if (timeDesc)
                    sql += " DESC";
            }

            return _database.Sql().ExecuteWithParams(sql, customerId)
                .AutoMap<CustomerHistoryEntryModel>()
                .AsReadOnly();
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