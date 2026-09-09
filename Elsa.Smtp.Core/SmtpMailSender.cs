using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Elsa.Common.Logging;
using Elsa.Smtp.Core.Database;
using MailKit.Net.Smtp;
using MimeKit;

namespace Elsa.Smtp.Core
{
    public class SmtpMailSender : IMailSender
    {
        private static readonly Regex _nonContentHtmlRegex = new Regex(
            @"<(script|style)[^>]*>.*?</\1>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex _htmlLineBreakRegex = new Regex(
            @"<(br\s*/?|/p|/div|/li|/tr|/h[1-6])\s*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _htmlTagRegex = new Regex(
            @"<[^>]+>",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex _extraLineBreakRegex = new Regex(
            @"(\r?\n\s*){3,}",
            RegexOptions.Compiled);

        private readonly SmtpSettings _allSettings;
        private readonly ILog _log;
        private readonly IRecipientListsRepository _recipientListsRepository;
        private readonly IMailTemplateRenderer _mailTemplateRenderer;

        private readonly DebugMailSender _debugMailSender;

        public SmtpMailSender(
            SmtpSettings settings,
            ILog log,
            IRecipientListsRepository recipientListsRepository,
            IMailTemplateRenderer mailTemplateRenderer)
        {
            _allSettings = settings;
            _log = log;
            _recipientListsRepository = recipientListsRepository;
            _mailTemplateRenderer = mailTemplateRenderer;

            _debugMailSender = new DebugMailSender(log, mailTemplateRenderer);
        }

        public void Send(SenderMailboxType mailbox, string to, string subject, string body, params string[] attachmentFiles)
        {
            Send(mailbox, new[] {to}, subject, body, false, attachmentFiles);

            try
            {
                _debugMailSender.Send(mailbox, to, subject, body, attachmentFiles);
            }
            catch (Exception ex) { _log.Error("Failed to send debug e-mail", ex); }
        }

        public void Send(SenderMailboxType mailbox, string to, MailTemplateContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Send(mailbox, new[] { to }, content.Subject, content.Body, content.IsHtml, new string[0]);

            try
            {
                _debugMailSender.Send(mailbox, to, content);
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
            
            Send(mailbox, recipients, subject, body, false, attachmentFiles);
        }

        public void Send(SenderMailboxType mailbox, string to, string templateTypeName, Dictionary<string, string> values)
        {
            var content = _mailTemplateRenderer.Render(templateTypeName, values);
            Send(mailbox, to, content);
        }

        private void Send(
            SenderMailboxType mailbox,
            IEnumerable<string> to,
            string subject,
            string body,
            bool isHtml,
            string[] attachemntFiles)
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

                var builder = isHtml
                    ? new BodyBuilder { HtmlBody = body, TextBody = HtmlToPlainText(body) }
                    : new BodyBuilder { TextBody = body };

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

        private static string HtmlToPlainText(string html)
        {
            var withoutNonContent = _nonContentHtmlRegex.Replace(html ?? string.Empty, string.Empty);
            var withLineBreaks = _htmlLineBreakRegex.Replace(withoutNonContent, "\r\n");
            var withoutTags = _htmlTagRegex.Replace(withLineBreaks, string.Empty);
            var decoded = WebUtility.HtmlDecode(withoutTags);
            return _extraLineBreakRegex.Replace(decoded, "\r\n\r\n").Trim();
        }
    }
}
