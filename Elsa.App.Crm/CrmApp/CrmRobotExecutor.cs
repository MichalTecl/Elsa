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

        public List<RobotExecutionResult> Execute(int robotId)
        {
            var tagIndex = _customerTags.GetTagTypes(false, false).ToDictionary(t => t.Id, t => t);

            var result = new List<RobotExecutionResult>();
                        
            _log.Info($"Loading robot data by id={robotId}");
            var robot = _filtersRepository.GetAllRobots(false).FirstOrDefault(r => r.Id == robotId).Ensure();

            _log.Info($"Deserializing the filter: {robot.JsonData}");

            var filter = JsonConvert.DeserializeObject<DistributorGridFilter>(robot.JsonData);

            _log.Info("Filter deserialised, executing:");

            var filterMatchingIds = _distributorsRepository.GetDistributors(filter, null, null, null, true).Select(r => r.Id).ToList();

            _log.Info($"Got {filterMatchingIds.Count} of ids, starting processing...");

            var allDistributors = _distributorsRepository.GetAllDistributors();

            void trySetTag(int customerId, int tagTypeId, bool remove)
            {
                var distributor = allDistributors.FirstOrDefault(d => d.Id == customerId);
                if (distributor == null)
                {
                    _log.Error($"Unexpected distributorId={customerId}");
                    return;
                }
                                
                if(distributor.TagTypeIds.Contains(tagTypeId) != remove)
                {
                    return;
                }

                if (!tagIndex.TryGetValue(tagTypeId, out var tagModel))
                    throw new InvalidOperationException($"Invalid tag type id '{tagTypeId}'");

                result.Add(new RobotExecutionResult 
                {
                    RobotId = robotId,
                    RobotName = robot.Name,
                    CustomerName = distributor.Name,
                    TagTypeName = tagModel.Name,
                    Added = !remove
                });

                _log.Info($"Going to {(remove ? "un" : "")}assing tag TagTypeId={tagTypeId}, Name='{tagModel.Name}' {(remove ? "from" : "to")} customer Id={distributor.Id} Name={distributor.Name}");

                if (remove)
                    _customerTags.Unassign(new[] { distributor.Id }, tagTypeId);
                else
                    _customerTags.Assign(new[] { distributor.Id }, tagTypeId);
            }
                        
            foreach (var distributor in allDistributors)
            {
                if (filterMatchingIds.Contains(distributor.Id))
                {
                    // Match

                    if (robot.FilterMatchSetsTag)
                    {
                        trySetTag(distributor.Id, robot.TagTypeId, false);
                    }

                    if (robot.FilterMatchRemovesTag)
                    {
                        trySetTag(distributor.Id, robot.TagTypeId, true);
                    }                  
                }
                else
                {
                    // Unmatch

                    if (robot.FilterUnmatchSetsTag)
                    {
                        trySetTag(distributor.Id, robot.TagTypeId, false);
                    }

                    if (robot.FilterUnmatchRemovesTags)
                    {
                        trySetTag(distributor.Id, robot.TagTypeId, true);
                    }
                }
            }

            _log.Info($"Robot {robot.Name} executed, result = {result.Count} changes");

            return result;
        }

        public class RobotExecutionResult
        {
            public int RobotId { get; set; }
            public string RobotName { get; set; }
            public string CustomerName { get; set; }
            public string TagTypeName { get; set; }
            public bool Added { get; set; }
        }

    }
}
