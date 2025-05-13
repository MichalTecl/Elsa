using Elsa.App.Crm.CrmApp;
using Elsa.App.Crm.Entities;
using Elsa.App.Crm.Repositories;
using Elsa.Commerce.Core.Crm;
using Elsa.Common.Caching;
using Elsa.Common.Logging;
using Elsa.Jobs.Common;
using Elsa.Smtp.Core;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
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
        private readonly CustomerTagRepository _customerTagRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IMailSender _mailSender;


        public RunCrmRobotsJob(IDatabase db, 
            ILog log, 
            DistributorFiltersRepository filtersRepository, 
            CrmRobotExecutor executor, 
            ICache cache, 
            CustomerTagRepository customerTagRepository,
            ICustomerRepository customerRepository, 
            IMailSender mailSender)
        {
            _db = db;
            _log = log;
            _filtersRepository = filtersRepository;
            _executor = executor;
            _cache = cache;
            _customerTagRepository = customerTagRepository;
            _customerRepository = customerRepository;
            _mailSender = mailSender;
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

                    _executor.Execute(robot.Id, allResults);

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

            var robotIndex = _filtersRepository.GetAllRobots(false).ToDictionary(r => r.Id, r => r);

            var mailTexts = CreateResultTexts(allResults, robotIndex);

            SendEmails(mailTexts, robotIndex);
        }

        private void SendEmails(Dictionary<RobotExecutionResult, string> mailTexts, Dictionary<int, ICrmRobot> robots)
        {
            

            var recipientMails = new Dictionary<string, StringBuilder>();

            foreach(var mailText in mailTexts)
            {
                if (!robots.TryGetValue(mailText.Key.RobotId, out var robot))
                    throw new Exception("Unexpected robot id");

                foreach(var recipient in robot.GetRecipients(_allMailsPeek))
                {
                    if (!recipientMails.TryGetValue(recipient, out var target))
                    {
                        target = new StringBuilder();
                        recipientMails.Add(recipient, target);
                    }

                    target.AppendLine(mailText.Value);
                }
            }

            _log.Info($"Collected e-mails for {recipientMails.Count} recipient(s)");

            foreach(var rm in recipientMails)
            {
                try
                {
                    _log.Info($"Sending e-mail to {rm.Key}");

                    _mailSender.Send(rm.Key, $"CRM Roboti provedli změny", rm.Value.ToString());

                    _log.Info("email sent");
                }
                catch (Exception ex)
                {
                    _log.Error($"Email sending failed recipient={rm.Key}", ex);
                }
            }
        }

        private Dictionary<RobotExecutionResult, string> CreateResultTexts(List<RobotExecutionResult> allResults, Dictionary<int, ICrmRobot> robotIndex)
        {
            _log.Info($"Got {allResults.Count} results to generate e-mail texts");

            var texts = new Dictionary<RobotExecutionResult, string>();

            var tagIndex = _customerTagRepository.GetTagTypes(null).ToDictionary(r => r.Id, r => r);
            var customerNameIndex = _customerRepository.GetDistributorNameIndex();

            foreach(var result in allResults.Where(r => r.AddedCustomers.Count > 0 || r.RemovedCustomers.Count > 0))
            {
                if (!robotIndex.TryGetValue(result.RobotId, out var robot))
                    throw new ArgumentException($"Unknown robotId {result.RobotId}");

                if(!tagIndex.TryGetValue(result.TagTypeId, out var tag))
                    throw new ArgumentException($"Unknown tagTypeId {result.TagTypeId}");

                _log.Info($"Generating mail text for robot {robot.Name}");

                var sb = new StringBuilder();
                
                void fillCustomerList(IEnumerable<int> ids)
                {
                    foreach(var id in ids)
                    {
                        if (!customerNameIndex.TryGetValue(id, out var customer))
                            throw new ArgumentException($"Unknown customerId {id}");

                        sb.Append("   ").AppendLine(customer);
                    }

                    sb.AppendLine();
                }

                if (result.AddedCustomers.Count > 0)
                {
                    sb.Append("Robot '").Append(robot.Name).Append("' přidal štítek '").Append(tag.Name).AppendLine("' těmto zákazníkům:");
                    fillCustomerList(result.AddedCustomers);
                }

                if (result.RemovedCustomers.Count > 0)
                {
                    sb.Append("Robot '").Append(robot.Name).Append("' odebral štítek '").Append(tag.Name).AppendLine("' těmto zákazníkům:");
                    fillCustomerList(result.RemovedCustomers);
                }

                texts[result] = sb.ToString();                
            }

            _log.Info($"Generated {texts.Count} e-mail texts");

            return texts;            
        }
    }
}
