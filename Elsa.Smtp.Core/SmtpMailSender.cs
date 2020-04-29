using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Common.Logging;
using Elsa.Smtp.Core.Database;
using MailKit.Net.Smtp;
using MimeKit;

namespace Elsa.Smtp.Core
{
    public class SmtpMailSender : IMailSender
    {
        private readonly SmtpSettings m_settings;
        private readonly ILog m_log;
        private readonly IRecipientListsRepository m_recipientListsRepository;

        public SmtpMailSender(SmtpSettings settings, ILog log, IRecipientListsRepository recipientListsRepository)
        {
            m_settings = settings;
            m_log = log;
            m_recipientListsRepository = recipientListsRepository;
        }

        public void Send(string to, string subject, string body)
        {
            Send(new[] {to}, subject, body);
        }

        public void SendToGroup(string groupName, string subject, string body)
        {
            var recipients = m_recipientListsRepository.GetRecipients(groupName).ToList();

            if (!recipients.Any())
            {
                m_log.Error($"No recipients for group '{groupName}'");
                return;
            }
            
            Send(recipients, subject, body);
        }

        private void Send(IEnumerable<string> to, string subject, string body)
        {
            var addresses = to.ToList();

            m_log.Info($"Sending e-mail to: {string.Join(";", addresses)}, subject: {subject}");

            try
            {
                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress(m_settings.SenderName, m_settings.SenderAddress));
                mailMessage.To.AddRange(addresses.Select(t => new MailboxAddress(t, t)) );
                mailMessage.Subject = subject;
                mailMessage.Body = new TextPart("plain")
                {
                    Text = body
                };

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Connect(m_settings.SmtpHost, m_settings.SmtpPort, true);
                    smtpClient.Authenticate(m_settings.SenderAddress, m_settings.SenderPassword);
                    smtpClient.Send(mailMessage);
                    smtpClient.Disconnect(true);
                }

                m_log.Info("Sent");
            }
            catch (Exception ex)
            {
                m_log.Error($"Sending e-mail to: {to}, subject: {subject} failed", ex);
                throw;
            }
        }
    }
}
