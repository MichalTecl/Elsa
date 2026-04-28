using MimeKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Elsa.Jobs.CrmMailPull.Infrastructure
{
    public class MimeMessageWrapper
    {
        private readonly MimeMessage _message;

        public MimeMessageWrapper(MimeMessage message)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));

            Subject = Truncate(NormalizeSubject(_message.Subject), 1000);

            BodyPlainText = ExtractPlainTextPreferSafe(_message);

            MessageUid = Truncate(CalcMessageUid(_message, BodyPlainText), 1000);
            ConversationUid = Truncate(CalcConversationUid(_message, MessageUid), 1000);

            Sender = Truncate(GetEffectiveSenderDisplay(_message), 1000);
        }

        public MimeMessage Message => _message;

        public string Sender { get; }

        /// <summary>
        /// Calculated unique identifier of the message to deduplicate for example the same message in multiple folders/mailbixes etc
        /// Fallback mechansms are intended to prefer duplicities over losing two similar messages
        /// max. len 1000 chars
        /// </summary>
        public string MessageUid { get; }

        /// <summary>
        /// Calculated unique identifier of a conversation where this message belongs.
        /// Fallback mechanisms are intended to prefer splitting a thread over joining independent conversations
        /// max len 1000 chars
        /// </summary>
        public string ConversationUid { get; }

        /// <summary>
        /// Subject, max len 1000 chars
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Plain text extracted from the message body.
        /// Fallback mechanisms are intended to prefer leaving mess in the final text over losing important parts of text
        /// Must fit to Nvarchar(max)
        /// </summary>
        public string BodyPlainText { get; }

        // -------------------------
        // MessageUid (dedupe)
        // -------------------------
        private static string CalcMessageUid(MimeMessage m, string bodyPlainText)
        {
            // Strong key: Message-Id
            var msgId = NormalizeMessageId(m.MessageId);
            if (!string.IsNullOrWhiteSpace(msgId))
                return "MID:" + msgId;

            // Fallback (conservative): hash of stable-ish fields.
            // This is intentionally conservative to avoid incorrectly deduping two different mails.
            // If in doubt, it will create different IDs (duplicates are preferable).
            var from = NormalizeMailboxList(m.From);
            var date = NormalizeDateForHash(m.Date);
            var subj = NormalizeSubject(m.Subject);

            // Optional: use a small chunk of body text to reduce collisions,
            // but keep it small to avoid heavy processing.
            var bodySnippet = MakeStableSnippet(bodyPlainText, maxChars: 256);

            var payload = $"FALLBACK|FROM={from}|DATE={date}|SUBJ={subj}|B={bodySnippet}";
            return "F:" + Sha256Base64Url(payload);
        }

        // -------------------------
        // ConversationUid (threading)
        // -------------------------
        private static string CalcConversationUid(MimeMessage m, string messageUid)
        {
            // Strongest: References root
            var refs = ParseMessageIdList(m.Headers["References"]);
            if (refs.Count > 0)
                return refs[0]; // root of thread

            // Next: In-Reply-To
            var irt = NormalizeMessageId(m.Headers["In-Reply-To"]);
            if (!string.IsNullOrWhiteSpace(irt))
                return irt;

            // Root message with Message-Id should share the same conversation key
            // that replies reference via References / In-Reply-To.
            var msgId = NormalizeMessageId(m.MessageId);
            if (!string.IsNullOrWhiteSpace(msgId))
                return msgId;

            // Conservative fallback: seed per-message (never merges unrelated conversations)
            // This guarantees we don't accidentally merge independent conversations.
            // It may split a real conversation if headers are missing -> acceptable per your rule.
            return "S:" + messageUid;
        }

        // -------------------------
        // BodyPlainText extraction
        // -------------------------
        private static string ExtractPlainTextPreferSafe(MimeMessage m)
        {
            // Prefer text/plain if present
            if (!string.IsNullOrWhiteSpace(m.TextBody))
                return NormalizeBodyText(m.TextBody);

            // Fallback to html -> text
            if (!string.IsNullOrWhiteSpace(m.HtmlBody))
                return NormalizeBodyText(HtmlToText(m.HtmlBody));

            // Last resort: find any TextPart
            var textPart = m.BodyParts.OfType<TextPart>().FirstOrDefault(tp => tp.IsPlain || tp.IsHtml);
            if (textPart != null)
            {
                var text = textPart.IsPlain ? textPart.Text : HtmlToText(textPart.Text);
                return NormalizeBodyText(text);
            }

            return string.Empty;
        }

        private static string NormalizeBodyText(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            // Normalize newlines
            s = s.Replace("\r\n", "\n").Replace("\r", "\n");

            // Keep "mess" rather than over-cleaning; just trim excessive trailing whitespace.
            // Collapse insane runs of blank lines a bit.
            s = Regex.Replace(s, @"\n{6,}", "\n\n\n", RegexOptions.CultureInvariant);

            return s.Trim();
        }

        // Very lightweight HTML->text to avoid losing content (prefers mess over loss).
        private static string HtmlToText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Remove script/style (safe to drop)
            html = Regex.Replace(html, @"<script[\s\S]*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            html = Regex.Replace(html, @"<style[\s\S]*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            // Newline-ish tags
            html = Regex.Replace(html, @"<(br|br\s*/)\s*>", "\n", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            html = Regex.Replace(html, @"</p\s*>", "\n\n", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            html = Regex.Replace(html, @"</div\s*>", "\n", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            html = Regex.Replace(html, @"</tr\s*>", "\n", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            html = Regex.Replace(html, @"</td\s*>", "\t", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            // Strip tags
            html = Regex.Replace(html, @"<[^>]+>", "", RegexOptions.CultureInvariant);

            // Decode entities
            html = System.Net.WebUtility.HtmlDecode(html);

            return html;
        }

        // -------------------------
        // Normalization helpers
        // -------------------------
        private static string NormalizeMessageId(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return string.Empty;

            // Typical forms: "<abc@domain>" but sometimes without <>
            messageId = messageId.Trim();
            if (messageId.StartsWith("<") && messageId.EndsWith(">") && messageId.Length > 2)
                messageId = messageId.Substring(1, messageId.Length - 2);

            // Lowercase is safe for comparison; msg-id is case-sensitive per spec-ish,
            // but in practice case-insensitive dedupe is what you want.
            messageId = messageId.Trim().ToLowerInvariant();

            // Keep only reasonable chars to avoid header injection artifacts
            messageId = Regex.Replace(messageId, @"\s+", "", RegexOptions.CultureInvariant);

            return messageId;
        }

        private static List<string> ParseMessageIdList(string headerValue)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(headerValue))
                return list;

            // Extract tokens like <...>
            foreach (Match m in Regex.Matches(headerValue, @"<([^>]+)>", RegexOptions.CultureInvariant))
            {
                var id = NormalizeMessageId(m.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(id))
                    list.Add(id);
            }

            // If none found, try whole header as single id (rare)
            if (list.Count == 0)
            {
                var single = NormalizeMessageId(headerValue);
                if (!string.IsNullOrWhiteSpace(single))
                    list.Add(single);
            }

            // Keep order; dedupe within list
            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string NormalizeSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return string.Empty;

            subject = subject.Trim();

            // Remove common reply/forward prefixes repeatedly, conservative
            // (won't try to be perfect in all languages)
            while (true)
            {
                var s2 = Regex.Replace(subject, @"^\s*(re|fw|fwd)\s*:\s*", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (s2 == subject) break;
                subject = s2;
            }

            // Collapse whitespace
            subject = Regex.Replace(subject, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
            return subject;
        }

        private static string NormalizeMailboxList(InternetAddressList list)
        {
            if (list == null || list.Count == 0)
                return string.Empty;

            // Expand groups; take mailbox addresses; normalize; sort for stability.
            var emails = new List<string>();
            foreach (var ia in list)
            {
                if (ia is MailboxAddress mb)
                {
                    if (!string.IsNullOrWhiteSpace(mb.Address))
                        emails.Add(mb.Address.Trim().ToLowerInvariant());
                }
                else if (ia is GroupAddress grp && grp.Members != null)
                {
                    foreach (var member in grp.Members.OfType<MailboxAddress>())
                    {
                        if (!string.IsNullOrWhiteSpace(member.Address))
                            emails.Add(member.Address.Trim().ToLowerInvariant());
                    }
                }
            }

            return string.Join(",", emails.Distinct().OrderBy(x => x, StringComparer.Ordinal).Take(50));
        }

        private static string NormalizeDateForHash(DateTimeOffset dto)
        {
            // Use minute precision to reduce tiny timezone/seconds differences, but keep enough specificity.
            // Using UTC makes it stable.
            var utc = dto.ToUniversalTime();
            utc = new DateTimeOffset(
                utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, TimeSpan.Zero);

            return utc.ToString("yyyy-MM-ddTHH:mmZ", CultureInfo.InvariantCulture);
        }

        private static string MakeStableSnippet(string s, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            s = s.Replace("\r\n", "\n").Replace("\r", "\n");
            s = Regex.Replace(s, @"\s+", " ", RegexOptions.CultureInvariant).Trim();

            if (s.Length > maxChars)
                s = s.Substring(0, maxChars);

            return s;
        }

        private static string Sha256Base64Url(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
                var b64 = Convert.ToBase64String(bytes);

                // Base64Url
                b64 = b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
                return b64;
            }
        }

        private static string Truncate(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= maxLen ? s : s.Substring(0, maxLen);
        }

        private static string GetEffectiveSenderDisplay(MimeMessage message)
        {
            if (message == null)
                return null;

            // 1) Prefer explicit Sender header
            var mailbox = message.Sender as MailboxAddress;

            // 2) Fallback to first From mailbox
            if (mailbox == null)
                mailbox = message.From?.OfType<MailboxAddress>()?.FirstOrDefault();

            if (mailbox == null || string.IsNullOrWhiteSpace(mailbox.Address))
                return null;

            var email = mailbox.Address.Trim();
            var name = mailbox.Name?.Trim();

            if (!string.IsNullOrWhiteSpace(name))
                return $"{name} <{email}>";

            return email;
        }
    }
}
