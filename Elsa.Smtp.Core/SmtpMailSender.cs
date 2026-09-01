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
        private readonly SmtpSettings _allSettings;
        private readonly ILog _log;
        private readonly IRecipientListsRepository _recipientListsRepository;

        private readonly DebugMailSender _debugMailSender;

        public SmtpMailSender(SmtpSettings settings, ILog log, IRecipientListsRepository recipientListsRepository)
        {
            _allSettings = settings;
            _log = log;
            _recipientListsRepository = recipientListsRepository;

            _debugMailSender = new DebugMailSender(log);
        }

        public void Send(SenderMailboxType mailbox, string to, string subject, string body, params string[] attachmentFiles)
        {
            Send(mailbox, new[] {to}, subject, body, attachmentFiles);

            try
            {
                _debugMailSender.Send(mailbox, to, subject, body, attachmentFiles);
            }
            catch (Exception ex) { _log.Error("Failed to send debug e-mail", ex); }
        }

        public void SendToGroup(SenderMailboxType mailbox, string groupName, string subject, string body, params string[] attachmentFiles)
        {
            var recipients = _recipientListsRepository.GetRecipients(groupName).ToList();

            if (!recipients.Any())
            {
                _log.Error($"No recipients for group '{groupName}'");
                return;
            }
            
            Send(mailbox, recipients, subject, body, attachmentFiles);
        }

        private void Send(SenderMailboxType mailbox, IEnumerable<string> to, string subject, string body, string[] attachemntFiles)
        {
            var addresses = to.ToList();

            _log.Info($"Sending [{mailbox.TypeName}] e-mail to: {string.Join(";", addresses)}, subject: {subject}");

            try
            {
                var settings = mailbox.MapSettings(_allSettings);

                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress(settings.SenderName, settings.SenderAddress));
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
                    smtpClient.Connect(settings.SmtpHost, settings.SmtpPort, true);
                    smtpClient.Authenticate(settings.SenderAddress, settings.SenderPassword);
                    smtpClient.Send(mailMessage);
                    smtpClient.Disconnect(true);
                }

                _log.Info("Sent");
            }
            catch (Exception ex)
            {
                _log.Error($"Sending e-mail to: {to}, subject: {subject} failed", ex);
                throw;
            }            
        }
    }
}
