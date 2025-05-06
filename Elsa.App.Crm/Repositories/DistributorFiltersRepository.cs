using Elsa.App.Crm.Entities;
using Elsa.App.Crm.Model;
using Elsa.Commerce.Core.Crm;
using Elsa.Common.Caching;
using Elsa.Common.DbUtils;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Input;

namespace Elsa.App.Crm.Repositories
{
    public class DistributorFiltersRepository
    {
        public const string FilterTextProcedureParameterName = "@filterText";

        private readonly IDatabase _db;
        private readonly ICache _cache;
        private readonly IProcedureLister _procedureLister;
        private readonly ICustomerRepository _customerRepository;
        private readonly ISession _session;

        public DistributorFiltersRepository(IDatabase db, ICache cache, IProcedureLister procedureLister, ICustomerRepository customerRepository, ISession session)
        {
            _db = db;
            _cache = cache;
            _procedureLister = procedureLister;
            _customerRepository = customerRepository;
            _session = session;
        }

        public List<DistributorFilterModel> GetFilters()
        {
            return _cache.ReadThrough("distributorFilters"
                , TimeSpan.FromHours(1)
                , () => {

                    var sps = _procedureLister.ListProcedures("crmfilter_%");

                    return sps.Select(s => new DistributorFilterModel(s)).OrderBy(f => f.Title).ToList();
                });
        }

        public FilterExecutionResult Execute(DistributorFilterModel clientData)
        {
            return _cache.ReadThrough(
                clientData.GetCacheKey(), 
                TimeSpan.FromMinutes(5), 
                () =>
                {
                    var filter = GetFilters().Where(f => f.ProcedureName == clientData.ProcedureName).Single();

                    var call = _db.Sql().Call(clientData.ProcedureName);
                                        
                    foreach (var parameterDefiniton in filter.Parameters)
                    {
                        var clientParam = clientData.Parameters.Single(p => p.Name == parameterDefiniton.Name);

                        call = call.WithParam(parameterDefiniton.Name, clientParam.Value);
                    }

                    SqlParameter filterTextParameter = null;
                    if (filter.HasFilterTextParameter)
                    {
                        filterTextParameter = new SqlParameter();
                        filterTextParameter.ParameterName = FilterTextProcedureParameterName;
                        filterTextParameter.Direction = System.Data.ParameterDirection.Output;
                        filterTextParameter.SqlDbType = SqlDbType.NVarChar;
                        filterTextParameter.Size = 1000;
                        call = call.WithParam(filterTextParameter);                         
                    }

                    var ids = new HashSet<int>(call.MapRows<int>(dr => dr.GetInt32(0)));

                    if (clientData.Inverted)
                    {
                        var allIds = GetAllUnfilteredDistributorIds();
                        ids = new HashSet<int>(allIds.Where(i => !ids.Contains(i)));
                    }

                    var result = new FilterExecutionResult { Ids = ids };

                    if (filter.HasFilterTextParameter)
                    {
                        result.FilterText = filterTextParameter.Value?.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(result.FilterText)) 
                    {
                        result.FilterText = $"{clientData.Title} {string.Join(",", clientData.Parameters.Select(p => StringUtil.Limit(p.Value, 20, "...")))}";
                    }
                                        
                    return result;
                });
        }

        public List<ICustomDistributorFilter> GetCustomFilters()
        {
            return _cache.ReadThrough(GetSavedFiltersCacheKey(),
                TimeSpan.FromHours(1),
                () => _db.SelectFrom<ICustomDistributorFilter>()
                .Where(f => f.AuthorId == _session.User.Id)
                .Execute()
                .ToList());
        }

        public void DeleteCustomFilter(int id)
        {
            var filter = GetCustomFilters().FirstOrDefault(f => f.Id == id).Ensure();

            _db.Delete(filter);
            _cache.Remove(GetSavedFiltersCacheKey());
        }

        public void SaveCustomFilter(int? id, string name, string jsonData)
        {
            var sameName = GetCustomFilters().FirstOrDefault(f => f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (sameName != null && sameName.Id != id)
                throw new ArgumentException($"Pod stejným názvem už byl uložen jiný filtr");

            ICustomDistributorFilter record = null;
            if (id != null)
            {
                record = GetCustomFilters().FirstOrDefault(f => f.Id == id).Ensure();
            }

            record = record ?? _db.New<ICustomDistributorFilter>(f =>
            {
                f.Created = DateTime.Now;
                f.AuthorId = _session.User.Id;                
            });

            record.Name = name;
            record.JsonData = jsonData;

            _db.Save(record);

            _cache.Remove(GetSavedFiltersCacheKey());
        }

        private string GetSavedFiltersCacheKey() => $"savedCrmFilters_{_session.User.Id}";

        public List<ICrmRobot> GetAllRobots(bool activeOnly)
        {
            return _cache.ReadThrough("crmRobots", TimeSpan.FromHours(1), () => {

                var query = _db.SelectFrom<ICrmRobot>()
                .Join(r => r.Author)
                .OrderBy(r => r.SequenceOrder);

                if (activeOnly) {
                    var now = DateTime.Now;

                    query = query
                    .Where(r => r.ActiveFrom <= now)
                    .Where(r => r.ActiveTo == null || r.ActiveTo >= now);
                }

                return query.Execute().ToList();
            });
        }

        public ICrmRobot SaveRobot(int? id, Action<ICrmRobot> setup)
        {
            ICrmRobot record;
            using (var tx = _db.OpenTransaction())
            {
                record = (id == null
                            ? _db.New<ICrmRobot>()
                            : _db.SelectFrom<ICrmRobot>()
                                .Where(r => r.Id == id)
                                .Execute()
                                .FirstOrDefault())
                            .Ensure();

                if (record.AuthorId == default)
                    record.AuthorId = _session.User.Id;

                if (record.ActiveFrom == default)
                    record.ActiveFrom = DateTime.Now;

                if (record.Created == default)
                    record.Created = DateTime.Now;

                if (record.SequenceOrder == default)
                {
                    var all = GetAllRobots(false);
                    if (all.Count > 0)
                        record.SequenceOrder = all.Max(r => r.SequenceOrder) + 1;
                }

                record.Description = record.Description ?? string.Empty;
                record.NotifyMailList = record.NotifyMailList ?? string.Empty;

                setup(record);

                _db.Save(record);

                tx.Commit();
            }

            _cache.Remove("crmRobots");

            return record;
        }

        private HashSet<int> GetAllUnfilteredDistributorIds()
        {
            return _cache.ReadThrough("AllUnfilteredDistributors", TimeSpan.FromMinutes(5), () => {
                return _customerRepository.GetDistributorNameIndex().Keys.ToHashSet();
            });
        }
        
    }
}
