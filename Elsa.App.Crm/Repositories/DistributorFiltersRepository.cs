using Elsa.App.Crm.Model;
using Elsa.Commerce.Core.Crm;
using Elsa.Common.Caching;
using Elsa.Common.DbUtils;
using Elsa.Common.Utils;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Elsa.App.Crm.Repositories
{
    public class DistributorFiltersRepository
    {
        public const string FilterTextProcedureParameterName = "@filterText";

        private readonly IDatabase _db;
        private readonly ICache _cache;
        private readonly IProcedureLister _procedureLister;
        private readonly ICustomerRepository _customerRepository;

        public DistributorFiltersRepository(IDatabase db, ICache cache, IProcedureLister procedureLister, ICustomerRepository customerRepository)
        {
            _db = db;
            _cache = cache;
            _procedureLister = procedureLister;
            _customerRepository = customerRepository;
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

        private HashSet<int> GetAllUnfilteredDistributorIds()
        {
            return _cache.ReadThrough("AllUnfilteredDistributors", TimeSpan.FromMinutes(5), () => {
                return _customerRepository.GetDistributorNameIndex().Keys.ToHashSet();
            });
        }
    }
}
