using Elsa.Common.Utils;
using Elsa.Jobs.CrmMailPull.Entities;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Jobs.CrmMailPull.Infrastructure
{
    public class MessageFilter
    {
        private readonly List<IMailPullAddressBlacklist> _addressBlists;
        private readonly List<IMailContentBlacklist> _contentBlists;

        public MessageFilter(List<IMailPullAddressBlacklist> addressBlists, List<IMailContentBlacklist> contentBlists)
        {
            _addressBlists = addressBlists;
            _contentBlists = contentBlists;
        }

        public void ClassifyBlacklisted(RemoteMailMessageReferenceInfo reference)
        {
            if (IsBlacklisted(reference.Subject, null))
            {
                reference.IsFilteredOut = true;
                return;
            }

            var blistedMails = reference.ParticipantEmails
                .Where(participant => _addressBlists.Any(pattern => StringUtil.MatchStarWildcard(pattern.Pattern, participant)))
                .ToList();

            foreach (var blistedEmail in blistedMails)
                reference.ParticipantEmails.Remove(blistedEmail);

            if (reference.ParticipantEmails.Count == 0)
                reference.IsFilteredOut = true;
        }

        internal bool CheckIsBlacklisted(MimeMessageWrapper message)
        {
            // 0) Hard blacklist by "system/non-valuable" type (NO body heuristics)
            var mm = message.Message;

            if (IsDeliveryStatusNotification(mm)) return true;   // bounce/DSN
            if (IsReadReceiptOrMdn(mm)) return true;             // read receipt
            if (IsAutoReply(mm)) return true;                    // OOO/auto-reply
            if (IsListMessage(mm)) return true;                  // mailing lists/newsletters
            if (IsCalendarMessage(mm)) return true;              // meeting invites

            // 1) User-defined content blacklist (subject/body)
            if (IsBlacklisted(message.Subject, message.BodyPlainText))
                return true;

            return false;
        }

        private bool IsBlacklisted(string subject, string body)
        {
            bool hasSubject = !string.IsNullOrEmpty(subject);
            bool hasBody = !string.IsNullOrEmpty(body);

            bool match(string pattern, string value)
            {
                if (string.IsNullOrEmpty(pattern))
                    return true; // pattern není -> nefiltruje

                if (string.IsNullOrEmpty(value))
                    return false; // pattern je, ale hodnota není

                return StringUtil.MatchStarWildcard(pattern, value);
            }

            foreach (var rule in _contentBlists)
            {
                if (!hasSubject && !string.IsNullOrEmpty(rule.SubjectPattern)) continue;
                if (!hasBody && !string.IsNullOrEmpty(rule.BodyPattern)) continue;

                if (match(rule.SubjectPattern, subject) && match(rule.BodyPattern, body))
                    return true;
            }

            return false;
        }

        // -------------------------
        // Header-only "system" detectors (no body heuristics)
        // -------------------------

        private static bool IsAutoReply(MimeMessage m)
        {
            if (m == null) return false;

            var autoSubmitted = GetHeaderLower(m, "Auto-Submitted");
            if (!string.IsNullOrEmpty(autoSubmitted) &&
                (autoSubmitted.Contains("auto-replied") || autoSubmitted.Contains("auto-reply") || autoSubmitted.Contains("auto-generated")))
                return true;

            // Exchange/Outlook
            if (HasHeader(m, "X-Auto-Response-Suppress")) return true;

            // Other vendors
            if (HasHeader(m, "X-Autoreply")) return true;
            if (HasHeader(m, "X-Autorespond")) return true;
            if (HasHeader(m, "X-AutoReply-From")) return true;
            if (HasHeader(m, "X-AutoReply-To")) return true;

            var precedence = GetHeaderLower(m, "Precedence");
            if (precedence == "auto_reply" || precedence == "auto-reply" || precedence == "autoreply")
                return true;

            return false;
        }

        private static bool IsDeliveryStatusNotification(MimeMessage m)
        {
            if (m == null) return false;

            // Strong DSN: multipart/report; report-type=delivery-status
            var ct = m.Body?.ContentType;
            if (ct != null &&
                ct.MediaType.Equals("multipart", StringComparison.OrdinalIgnoreCase) &&
                ct.MediaSubtype.Equals("report", StringComparison.OrdinalIgnoreCase))
            {
                var reportType = ct.Parameters?["report-type"]?.ToLowerInvariant();
                if (reportType == "delivery-status")
                    return true;
            }

            if (HasHeader(m, "X-Failed-Recipients")) return true;
            if (HasHeader(m, "X-Delivery-Status")) return true;

            var returnPath = (m.Headers["Return-Path"] ?? string.Empty).Trim();
            if (returnPath == "<>")
                return true;

            return false;
        }

        private static bool IsReadReceiptOrMdn(MimeMessage m)
        {
            if (m == null) return false;

            // MDN headers
            if (HasHeader(m, "Disposition-Notification-To")) return true;
            if (HasHeader(m, "Return-Receipt-To")) return true;

            // MDN report: multipart/report; report-type=disposition-notification
            var ct = m.Body?.ContentType;
            if (ct != null &&
                ct.MediaType.Equals("multipart", StringComparison.OrdinalIgnoreCase) &&
                ct.MediaSubtype.Equals("report", StringComparison.OrdinalIgnoreCase))
            {
                var reportType = ct.Parameters?["report-type"]?.ToLowerInvariant();
                if (reportType == "disposition-notification")
                    return true;
            }

            return false;
        }

        private static bool IsListMessage(MimeMessage m)
        {
            if (m == null) return false;

            // RFC 2369 / common list headers
            if (HasHeader(m, "List-Id")) return true;
            if (HasHeader(m, "List-Unsubscribe")) return true;
            if (HasHeader(m, "List-Post")) return true;
            if (HasHeader(m, "List-Help")) return true;
            if (HasHeader(m, "List-Subscribe")) return true;
            if (HasHeader(m, "List-Owner")) return true;

            // Often used too (optional)
            var precedence = GetHeaderLower(m, "Precedence");
            if (precedence == "list")
                return true;

            return false;
        }

        private static bool IsCalendarMessage(MimeMessage m)
        {
            if (m == null) return false;

            // Common invites are text/calendar or multipart with a text/calendar part
            foreach (var part in m.BodyParts)
            {
                if (part is TextPart tp && tp.ContentType != null)
                {
                    if (tp.ContentType.MediaType.Equals("text", StringComparison.OrdinalIgnoreCase) &&
                        tp.ContentType.MediaSubtype.Equals("calendar", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            // Also handle the case where body itself is text/calendar
            if (m.Body is TextPart bodyTp && bodyTp.ContentType != null)
            {
                if (bodyTp.ContentType.MediaType.Equals("text", StringComparison.OrdinalIgnoreCase) &&
                    bodyTp.ContentType.MediaSubtype.Equals("calendar", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool HasHeader(MimeMessage m, string name)
            => !string.IsNullOrWhiteSpace(m?.Headers[name]);

        private static string GetHeaderLower(MimeMessage m, string name)
            => (m?.Headers[name] ?? string.Empty).Trim().ToLowerInvariant();

        // -------------------------
        // Participants from MimeMessage (for optional address blacklist in CheckIsBlacklisted)
        // -------------------------

        private static List<string> ExtractParticipantEmails(MimeMessage m)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddEmails(set, m.From);
            AddEmails(set, m.To);
            AddEmails(set, m.Cc);
            AddEmails(set, m.ReplyTo);

            return set.ToList();
        }

        private static void AddEmails(HashSet<string> set, InternetAddressList list)
        {
            if (set == null || list == null) return;

            foreach (var addr in list)
            {
                if (addr is MailboxAddress mb)
                {
                    if (!string.IsNullOrWhiteSpace(mb.Address))
                        set.Add(mb.Address.Trim());
                }
                else if (addr is GroupAddress grp && grp.Members != null)
                {
                    foreach (var member in grp.Members.OfType<MailboxAddress>())
                    {
                        if (!string.IsNullOrWhiteSpace(member.Address))
                            set.Add(member.Address.Trim());
                    }
                }
            }
        }
    }
}