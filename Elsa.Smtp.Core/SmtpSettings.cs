using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Configuration;

namespace Elsa.Smtp.Core
{
    [ConfigClass]
    public class SmtpSettings
    {
        [ConfigEntry("Mailer.Host", ConfigEntryScope.Project)]
        public string SmtpHost { get; set; }

        [ConfigEntry("Mailer.Port", ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public int SmtpPort { get; set; }

        [ConfigEntry("Mailer.SenderAddress", ConfigEntryScope.Project)]
        public string SenderAddress { get; set; }

        [ConfigEntry("Mailer.SenderName", ConfigEntryScope.Project)]
        public string SenderName { get; set; }

        [ConfigEntry("Mailer.SenderPassword", ConfigEntryScope.Project)]
        public string SenderPassword { get; set; }

        [ConfigEntry("CustomerFacingMailer.Host", ConfigEntryScope.Project)]
        public string CustomerFacingSmtpHost { get; set; }

        [ConfigEntry("CustomerFacingMailer.Port", ConfigEntryScope.Project, ConfigEntryScope.Global)]
        public int CustomerFacingSmtpPort { get; set; }

        [ConfigEntry("CustomerFacingMailer.SenderAddress", ConfigEntryScope.Project)]
        public string CustomerFacingSenderAddress { get; set; }

        [ConfigEntry("CustomerFacingMailer.SenderName", ConfigEntryScope.Project)]
        public string CustomerFacingSenderName { get; set; }

        [ConfigEntry("CustomerFacingMailer.SenderPassword", ConfigEntryScope.Project)]
        public string CustomerFacingSenderPassword { get; set; }

        public bool EnableSsl { get; set; } = true;

        public SmtpDeliveryMethod DeliveryMethod { get; set; } = SmtpDeliveryMethod.Network;


    }
}
