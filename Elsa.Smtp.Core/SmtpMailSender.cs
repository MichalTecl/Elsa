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

        public void Send(string to, string subject, string body, params string[] attachmentFiles)
        {
            Send(new[] {to}, subject, body, attachmentFiles);
        }

        public void SendToGroup(string groupName, string subject, string body, params string[] attachmentFiles)
        {
            var recipients = m_recipientListsRepository.GetRecipients(groupName).ToList();

            if (!recipients.Any())
            {
                m_log.Error($"No recipients for group '{groupName}'");
                return;
            }
            
            Send(recipients, subject, body, attachmentFiles);
        }

        private void Send(IEnumerable<string> to, string subject, string body, string[] attachemntFiles)
        {
            var addresses = to.ToList();

            m_log.Info($"Sending e-mail to: {string.Join(";", addresses)}, subject: {subject}");

            try
            {
                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress(m_settings.SenderName, m_settings.SenderAddress));
                mailMessage.To.AddRange(addresses.Select(t => new MailboxAddress(t, t)) );
                mailMessage.Subject = subject;

                var builder = new BodyBuilder {TextBody = body};

                foreach (var atf in attachemntFiles)
                {
                    builder.Attachments.Add(atf);
                }

                mailMessage.Body = builder.ToMessageBody();

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

            if (string.IsNullOrEmpty(m_settings.AllMailReceiver))
            {
                m_log.Info("Mailer.AllMailReceiver not set");
                return;
            }

            if (to.Contains(m_settings.AllMailReceiver))
            {
                return;
            }

            m_log.Info($"AllMailReceiver is set, but not included in receivers - Sending e-mail to: {m_settings.AllMailReceiver}, subject: {subject}");
            Send(m_settings.AllMailReceiver, subject, body);
        }
    }
}
