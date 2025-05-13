using Elsa.App.Crm.Controllers;
using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.CrmApp
{
    public class CrmRobotExecutor
    {
        private readonly ILog _log;
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly DistributorFiltersRepository _filtersRepository;
        private readonly DistributorsRepository _distributorsRepository;
        private readonly CustomerTagRepository _customerTags;

        public CrmRobotExecutor(ILog log, IDatabase database, ISession session, DistributorFiltersRepository filtersRepository, DistributorsRepository distributorsRepository, CustomerTagRepository customerTags)
        {
            _log = log;
            _database = database;
            _session = session;
            _filtersRepository = filtersRepository;
            _distributorsRepository = distributorsRepository;
            _customerTags = customerTags;
        }

        public void Execute(int robotId, List<RobotExecutionResult> target)
        {
            var tagIndex = _customerTags.GetTagTypes(null).ToDictionary(t => t.Id, t => t);
                                    
            _log.Info($"Loading robot data by id={robotId}");
            var robot = _filtersRepository.GetAllRobots(false).FirstOrDefault(r => r.Id == robotId).Ensure();

            _log.Info($"Deserializing the filter: {robot.JsonData}");

            var filter = JsonConvert.DeserializeObject<DistributorGridFilter>(robot.JsonData);

            _log.Info("Filter deserialised, executing:");

            var filterMatchingIds = _distributorsRepository.GetDistributors(filter, null, null, null, true).Select(r => r.Id).ToList();

            _log.Info($"Got {filterMatchingIds.Count} of ids, starting processing...");

            var allDistributors = _distributorsRepository.GetAllDistributors();

            int removalsCount = 0;
            int addsCount = 0;

            void trySetTag(int customerId, int? tagTypeId, bool remove)
            {
                if (tagTypeId == null)
                    return;

                var distributor = allDistributors.FirstOrDefault(d => d.Id == customerId);
                if (distributor == null)
                {
                    _log.Error($"Unexpected distributorId={customerId}");
                    return;
                }
                                
                if(distributor.TagTypeIds.Contains(tagTypeId.Value) != remove)
                {
                    return;
                }

                if (!tagIndex.TryGetValue(tagTypeId.Value, out var tagModel))
                    throw new InvalidOperationException($"Invalid tag type id '{tagTypeId}'");

                var result = target.FirstOrDefault(t => t.RobotId == robot.Id && t.TagTypeId == tagTypeId);

                if(result == null)
                {
                    result = new RobotExecutionResult(robot.Id, tagTypeId.Value);
                    target.Add(result);
                }
                                
                _log.Info($"Going to {(remove ? "un" : "")}assing tag TagTypeId={tagTypeId}, Name='{tagModel.Name}' {(remove ? "from" : "to")} customer Id={distributor.Id} Name={distributor.Name}");

                if (remove)
                {
                    _customerTags.Unassign(new[] { distributor.Id }, tagTypeId.Value);
                    result.RemovedCustomers.Add(distributor.Id);
                    removalsCount++;
                }
                else
                {
                    _customerTags.Assign(new[] { distributor.Id }, tagTypeId.Value);
                    result.AddedCustomers.Add(distributor.Id);
                    addsCount++;
                }
            }
                        
            foreach (var distributor in allDistributors)
            {
                if (filterMatchingIds.Contains(distributor.Id))
                {
                    // Match
                    trySetTag(distributor.Id, robot.FilterMatchSetsTagTypeId, false);
                    trySetTag(distributor.Id, robot.FilterMatchRemovesTagTypeId, true);
                }
                else
                {
                    // Unmatch
                    trySetTag(distributor.Id, robot.FilterUnmatchSetsTagTypeId, false);                    
                    trySetTag(distributor.Id, robot.FilterUnmatchRemovesTagTypeId, true);
                }
            }

            _log.Info($"Robot {robot.Name} executed, {addsCount} customer-tag pair(s) assigned; {removalsCount} customer-tag pair(s) unassigned");
        }

        public class RobotExecutionResult
        {
            public RobotExecutionResult(int robotId, int tagTypeId)
            {
                RobotId = robotId;
                TagTypeId = tagTypeId;
            }

            public int RobotId { get; }
            public int TagTypeId { get; }
            public List<int> AddedCustomers { get; } = new List<int>();
            public List<int> RemovedCustomers { get; } = new List<int>();
        }

    }
}
