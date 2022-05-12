using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Common.Logging;
using Elsa.Jobs.Common;
using Elsa.Jobs.SyncErpCustomers.Mailchimp;

namespace Elsa.Jobs.SyncErpCustomers
{
    public class CustomersSyncJob : IExecutableJob
    {
        private readonly IErpClientFactory m_erpClientFactory;
        private readonly ICustomerRepository m_customerRepository;
        private readonly ILog m_log;
        private readonly MailchimpClientConfig m_mcConfig;

        public CustomersSyncJob(IErpClientFactory erpClientFactory, ICustomerRepository customerRepository, ILog log, MailchimpClientConfig mcConfig)
        {
            m_erpClientFactory = erpClientFactory;
            m_customerRepository = customerRepository;
            m_log = log;
            m_mcConfig = mcConfig;
        }

        public void Run(string customDataJson)
        {
            m_customerRepository.SyncShadowCustomers();

            m_log.Info("Starting synchronization of customers from ERP");
            foreach (var erp in m_erpClientFactory.GetAllErpClients())
            {
                var erpCustomerModels = erp.LoadCustomers().ToList();
                if (!erpCustomerModels.Any())
                {
                    continue;
                }

                m_customerRepository.SyncCustomers(erpCustomerModels);
            }

            m_log.Info("Newsletter subscribers sync from MailChimp:");

            var mc = new MailchimpClient();

            var subscribers = Task.Run(() => mc.GetSubscribedMembers(m_mcConfig, m_log)).Result;

            m_log.Info($"Received {subscribers.Count} of newsletter subscribers");
            m_customerRepository.UpdateNewsletterSubscribersList("Mailchimp", subscribers);
            
            //m_log.Info($"Obtaining subscribers missing in the Mailchimp but subscribed on web:");
            //var toSubscribe = m_customerRepository.GetSubscribersToSync("Mailchimp");
            
        }
    }
}
