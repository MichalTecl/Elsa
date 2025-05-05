using Elsa.App.Crm.CrmApp;
using Elsa.App.Crm.Repositories;
using Elsa.Common.Caching;
using Elsa.Common.Logging;
using Elsa.Jobs.Common;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using static Elsa.App.Crm.CrmApp.CrmRobotExecutor;

namespace Elsa.Jobs.CrmRobots
{
    public class RunCrmRobotsJob : IExecutableJob
    {
        // TODO config
        private static readonly string[] _allMailsPeek = new[] { "mtecl.prg@gmail.com" };

        private readonly IDatabase _db;
        private readonly ILog _log;
        private readonly DistributorFiltersRepository _filtersRepository;
        private readonly CrmRobotExecutor _executor;
        private readonly ICache _cache;

        public RunCrmRobotsJob(IDatabase db, ILog log, DistributorFiltersRepository filtersRepository, CrmRobotExecutor executor, ICache cache)
        {
            _db = db;
            _log = log;
            _filtersRepository = filtersRepository;
            _executor = executor;
            _cache = cache;
        }

        public void Run(string customDataJson)
        {
            _log.Info("Starting CRM Robots execution job. Loading robots...");

            var robots = _filtersRepository.GetAllRobots(true);

            _log.Info($"loaded {robots.Count} active robots");

            var allResults = new List<RobotExecutionResult>();

            foreach (var robot in robots)
            {
                try
                {
                    _log.Info("Clearing cache");
                    _cache.Clear(); // There is so much cached stuff which may affect the robot execution... but it only happens in background job
                    _log.Info($"Executing robot {robot.Name}");

                    allResults.AddRange(_executor.Execute(robot.Id));

                    _log.Info($"robot ${robot.Name} executed successfully");
                }
                catch (Exception ex)
                {
                    _log.Error($"Execution of robot {robot.Name} failed", ex);
                }
            }

            _log.Info($"Crm robots execution complete - going to process {allResults.Count} tag assignment changes");

            if (allResults.Count == 0)
            {
                _log.Info("No changes - skipping mailing");
                return;
            }

            CollectMailNotificationSets(allResults);
        }

        private void CollectMailNotificationSets(List<RobotExecutionResult> allResults)
        {
            var robotIndex = _filtersRepository.GetAllRobots(false).ToDictionary(r => r.Id, r => r);

            IEnumerable<string> getRecipients(string mailList)
            {
                return string.IsNullOrWhiteSpace(mailList?.Trim())
                    ? _allMailsPeek
                    : mailList.Split(',', ';')
                    .Select(m => m.Trim())
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Select(m => m.ToLowerInvariant())
                    .Concat(_allMailsPeek)
                    .Distinct();
            }

            var recipientResults = new Dictionary<string, List<RobotExecutionResult>>();

            foreach (var result in allResults)
            {
                if (!robotIndex.TryGetValue(result.RobotId, out var robot))
                {
                    _log.Error($"Unexpected robot id '{result.RobotId}' in allResults");
                    continue;
                }

                foreach (var robotRecipient in getRecipients(robot.NotifyMailList))
                {
                    if (!recipientResults.TryGetValue(robotRecipient, out var target))
                    {
                        target = new List<RobotExecutionResult>();
                        recipientResults[robotRecipient] = target;
                    }

                    target.Add(result);
                }
            }

            _log.Info($"Collected {recipientResults.Count} of e-mail recipients");
        }
    }
}
