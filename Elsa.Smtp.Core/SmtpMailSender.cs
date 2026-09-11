using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Smtp.Core.Database;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Utils;

namespace Elsa.Smtp.Core
{
    public class SmtpMailSender : IMailSender
    {
        private const string DEV_EMAIL_RECIPIENT = "mtecl.prg@gmail.com";

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

        private static readonly Regex _htmlPictureRegex = new Regex(
            @"(?<prefix><img\b[^>]*?\ssrc\s*=\s*)(?<quote>['""])(?<source>.*?)(\k<quote>)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex _htmlPictureSourceRegex = new Regex(
            @"<img\b[^>]*?\ssrc\s*=",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

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
            try
            {
                _debugMailSender.Send(mailbox, to, subject, body, attachmentFiles);
            }
            catch (Exception ex) { _log.Error("Failed to send debug e-mail", ex); }

            Send(mailbox, new[] {to}, subject, body, false, attachmentFiles);
        }

        public void Send(SenderMailboxType mailbox, string to, MailTemplateContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            try
            {
                _debugMailSender.Send(mailbox, to, content);
            }
            catch (Exception ex) { _log.Error("Failed to send debug e-mail", ex); }

            Send(mailbox, new[] { to }, content.Subject, content.Body, content.IsHtml, new string[0]);
        }

        public void SendToGroup(SenderMailboxType mailbox, string groupName, string subject, string body, params string[] attachmentFiles)
        {
            var recipients = _recipientListsRepository.GetRecipients(groupName).ToList();

            if (!recipients.Any() && !AppEnvironment.IsDev)
            {
                _log.Error($"No recipients for group '{groupName}'");
                return;
            }
            
            if (AppEnvironment.IsDev)
            {
                try
                {
                    _debugMailSender.SendToGroup(mailbox, groupName, subject, body, attachmentFiles);
                }
                catch (Exception ex) { _log.Error("Failed to send debug e-mail", ex); }
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
            var requestedAddresses = to.ToList();
            var addresses = AppEnvironment.IsDev
                ? new List<string> { DEV_EMAIL_RECIPIENT }
                : requestedAddresses;

            var reroutedFrom = AppEnvironment.IsDev
                ? $" (dev redirect from: {string.Join(";", requestedAddresses)})"
                : string.Empty;
            _log.Info($"Sending [{mailbox.TypeName}] e-mail to: {string.Join(";", addresses)}{reroutedFrom}, subject: {subject}");

            try
            {
                var settings = mailbox.MapSettings(_allSettings);

                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress(settings.SenderName, settings.SenderAddress));
                mailMessage.To.AddRange(addresses.Select(t => new MailboxAddress(t, t)) );
                mailMessage.Subject = subject;

                var builder = new BodyBuilder();
                if (isHtml)
                {
                    var bodyWithPictures = EmbedPictures(builder, body);
                    builder.HtmlBody = MailTemplateQrCode.Embed(builder, bodyWithPictures);
                    builder.TextBody = HtmlToPlainText(body);
                }
                else
                {
                    builder.TextBody = body;
                }

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
                _log.Error($"Sending e-mail to: {string.Join(";", addresses)}, subject: {subject} failed", ex);
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

        private static string EmbedPictures(BodyBuilder builder, string html)
        {
            var htmlContent = html ?? string.Empty;
            if (_htmlPictureSourceRegex.Matches(htmlContent).Count != _htmlPictureRegex.Matches(htmlContent).Count)
            {
                throw new InvalidOperationException(
                    "Atribut src obrázku musí být uzavřený v jednoduchých nebo dvojitých uvozovkách.");
            }

            var contentIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return _htmlPictureRegex.Replace(htmlContent, match =>
            {
                var source = WebUtility.HtmlDecode(match.Groups["source"].Value);
                var picturePath = MailTemplatePictureStore.ResolvePath(source);

                if (!contentIds.TryGetValue(picturePath, out var contentId))
                {
                    var linkedPicture = builder.LinkedResources.Add(picturePath);
                    linkedPicture.ContentId = MimeUtils.GenerateMessageId();
                    contentId = linkedPicture.ContentId;
                    contentIds[picturePath] = contentId;
                }

                var quote = match.Groups["quote"].Value;
                return $"{match.Groups["prefix"].Value}{quote}cid:{contentId}{quote}";
            });
        }
    }
}
