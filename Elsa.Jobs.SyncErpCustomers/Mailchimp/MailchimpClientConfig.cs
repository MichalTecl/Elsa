using Elsa.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.SyncErpCustomers.Mailchimp
{
    [ConfigClass]
    public class MailchimpClientConfig
    {
        [ConfigEntry("MailchimpClient.ApiKey", ConfigEntryScope.Project)]
        public string ApiKey { get; set; }

        [ConfigEntry("MailchimpClient.ListName")]
        public string ListName { get; set; }
    }
}
