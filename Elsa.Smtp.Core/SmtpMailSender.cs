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
        private readonly SmtpSettings _settings;
        private readonly ILog _log;
        private readonly IRecipientListsRepository _recipientListsRepository;

        private readonly DebugMailSender _debugMailSender;

        public SmtpMailSender(SmtpSettings settings, ILog log, IRecipientListsRepository recipientListsRepository)
        {
            _settings = settings;
            _log = log;
            _recipientListsRepository = recipientListsRepository;

            _debugMailSender = new DebugMailSender(log);
        }

        public void Send(string to, string subject, string body, params string[] attachmentFiles)
        {
            Send(new[] {to}, subject, body, attachmentFiles);

            try
            {
                _debugMailSender.Send(to, subject, body, attachmentFiles);
            }
            catch (Exception ex) { _log.Error("Failed to send debug e-mail", ex); }
        }

        public void SendToGroup(string groupName, string subject, string body, params string[] attachmentFiles)
        {
            var recipients = _recipientListsRepository.GetRecipients(groupName).ToList();

            if (!recipients.Any())
            {
                _log.Error($"No recipients for group '{groupName}'");
                return;
            }
            
            Send(recipients, subject, body, attachmentFiles);
        }

        private void Send(IEnumerable<string> to, string subject, string body, string[] attachemntFiles)
        {
            var addresses = to.ToList();

            _log.Info($"Sending e-mail to: {string.Join(";", addresses)}, subject: {subject}");

            try
            {
                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderAddress));
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
                    smtpClient.Connect(_settings.SmtpHost, _settings.SmtpPort, true);
                    smtpClient.Authenticate(_settings.SenderAddress, _settings.SenderPassword);
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
