using Elsa.Common.Logging;
using MailChimp.Net;
using MailChimp.Net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.SyncErpCustomers.Mailchimp
{
    public class MailchimpClient
    {        
        public async Task<Dictionary<string, bool>> GetMembersSubscriptionStatus(MailchimpClientConfig cfg, ILog log)
        {
            var manager = new MailChimpManager(cfg.ApiKey);

            log.Info("Requesting MailChimp lists");
            var mailChimpListCollection = (await manager.Lists.GetAllAsync().ConfigureAwait(false)).ToList();

            log.Info($"Received {mailChimpListCollection.Count} lists. Looking for list.Name='{cfg.ListName}'");
            var list = mailChimpListCollection
                .FirstOrDefault(l => l.Name.Equals(cfg.ListName, StringComparison.InvariantCultureIgnoreCase))
                ?? throw new ArgumentException($"Mailchimp List \"{cfg.ListName}\" not found");

            log.Info($"Found list Name='{list.Name}' Id='{list.Id}'");

            var result = new Dictionary<string, bool>();

            const int limit = 500;
            int offset = 0;
            int receivedCount = 0;
            do
            {
                var mr = new MemberRequest
                {
                    Limit = limit,
                    Offset = offset
                };

                log.Info($"Requesting {limit} subscribers (offset={offset})");

                var members = (await manager.Members.GetAllAsync(list.Id, mr)).ToList();
                receivedCount = members.Count;
                offset += receivedCount;

                log.Info($"Received {receivedCount} subscribers");

                foreach(var mcMember in members)
                {
                    var mail = mcMember.EmailAddress.Trim().ToLowerInvariant();
                    result[mail] = mcMember.Status == MailChimp.Net.Models.Status.Subscribed;                    
                }

            } while (receivedCount == limit);

            log.Info($"Subscribers download complete - got {result.Count} subscribers");

            return result;
        }
    }

    public class McMemberModel 
    {
        public string Name { get; set; }

        public bool IsSubscriber { get; set; }

        public DateTime? SubscribeDt { get; set; }

        public DateTime? UnsubscribeDt { get; set; }
    }
}
