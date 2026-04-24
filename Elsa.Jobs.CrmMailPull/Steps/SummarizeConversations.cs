using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Integration.ChatGpt;
using Elsa.Integration.ChatGpt.Model;
using Elsa.Jobs.CrmMailPull.Entities;
using Elsa.Jobs.CrmMailPull.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Elsa.Jobs.CrmMailPull.Steps
{
    public class SummarizeConversations : IStep
    {
        private readonly ILog _log;
        private readonly MailPullRepository _repository;
        private readonly IChatGptClient _ai;

        public SummarizeConversations(ILog log, MailPullRepository repository, IChatGptClient ai)
        {
            _log = log;
            _repository = repository;
            _ai = ai;
        }

        public void Run(TimeoutCheck timeout)
        {
            const int minWordsForAiSummary = 50;

            while (true)
            {
                timeout.Check();

                var conversationId = _repository.GetNewestConversationIdMissingSummary();
                if (conversationId == null)
                {
                    _log.Info("No conversations missing summary");
                    return;
                }

                var conversationIdVal = conversationId.Value;
                _log.Info($"Summarizing conversation Id={conversationIdVal}");

                try
                {
                    var messages = _repository.GetConversationMessages(conversationIdVal);
                    var normalizedMessages = BuildNormalizedMessages(messages);
                    var conversationSubject = MakeFallbackSubject(normalizedMessages.Select(m => m.Subject).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)));
                    var sanitizedMessages = BuildSanitizedMessages(normalizedMessages);
                    var preparedMessages = BuildPromptMessages(sanitizedMessages);

                    if (preparedMessages.Count == 0)
                    {
                        _repository.SaveConversationSummary(
                            conversationIdVal,
                            conversationSubject,
                            "Konverzace neobsahovala pouzitelny text pro automaticke shrnuti.");
                        continue;
                    }

                    var conversationWordCount = CountWords(string.Join("\n\n", normalizedMessages.Select(m => m.Body)));
                    if (conversationWordCount < minWordsForAiSummary)
                    {
                        _log.Info($"Conversation {conversationIdVal} has only {conversationWordCount} words after cleanup, saving direct summary without AI");
                        _repository.SaveConversationSummary(
                            conversationIdVal,
                            conversationSubject,
                            BuildDirectSummary(normalizedMessages));
                        continue;
                    }

                    var prompt = BuildPrompt(preparedMessages);

                    _log.Info($"Prepared {preparedMessages.Count} message(s) for summarization, prompt length {prompt.Length} chars");

                    var response = _ai.Request(new OpenAiRequestBody
                    {
                        Temperature = 0.1f,
                        MaxTokens = 250, // Zvyseno z 160 – dava modelu dychaci prostor pro 2 vety
                        Messages = new List<OpenAiRequestBody.Message>
                        {
                            new OpenAiRequestBody.Message
                            {
                                Role = "system",
                                Content = BuildSystemPrompt()
                            },
                            new OpenAiRequestBody.Message
                            {
                                Role = "user",
                                Content = prompt
                            }
                        }
                    });

                    var firstChoice = response?.Choices?.FirstOrDefault();
                    var content = firstChoice?.Message?.Content;
                    if (string.Equals(firstChoice?.FinishReason, "content_filter", StringComparison.OrdinalIgnoreCase))
                    {
                        _log.Info($"Conversation {conversationIdVal} was blocked by content filter, saving direct summary without AI");
                        _repository.SaveConversationSummary(
                            conversationIdVal,
                            conversationSubject,
                            BuildDirectSummary(normalizedMessages));
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(content))
                        throw new InvalidOperationException("OpenAI returned empty response");

                    var parsed = ParseSummary(content, null, preparedMessages);
                    _repository.SaveConversationSummary(conversationIdVal, conversationSubject, parsed.Summary);

                    _log.Info($"Conversation {conversationIdVal} summarized");
                }
                catch (Exception ex)
                {
                    _log.Error($"Summarization failed for conversation {conversationIdVal}", ex);
                    break;
                }
            }
        }

        // -------------------------------------------------------------------------
        // Prompty
        // -------------------------------------------------------------------------

        private static string BuildSystemPrompt()
        {
            return
                "Jsi interni CRM asistent. Tvuj ukol je vytvorit strojovy zaznam e-mailove komunikace pro obchodnika.\n\n" +
                "IGNORUJ vzdy: pozdravy, podekování, omluvy, zdvorilostni fraze, podpisy, ujisteni o rychle odpovedi " +
                "a jakekoliv casti textu bez vecneho obsahu.\n\n" +
                "ZAZNAMENEJ vzdy: co konkretne zakaznik pozadoval nebo resil, co bylo dohodnuto nebo odeslano, " +
                "relevantni cisla (castky, mnozstvi, terminy, cisla objednavek), " +
                "otevrene ukoly a dalsi kroky.";
        }

        private static string BuildPrompt(List<PromptMessage> messages)
        {
            var sb = new StringBuilder(6000);

            sb.AppendLine("Vrat vystup POUZE v tomto formatu, bez cokoliv dalsiho:");
            sb.AppendLine("SubjectSummary: [max 100 znaku – tema konverzace, zadna slovesa]");
            sb.AppendLine("Summary: [1-2 vety – co bylo reseno, co byl vysledek nebo co je otevrene]");
            sb.AppendLine();
            sb.AppendLine("Pravidla pro Summary:");
            sb.AppendLine("- Pis jako interni poznamka, ne jako preklad e-mailu.");
            sb.AppendLine("- Zacni rovnou vecnou informaci, ne jmenem odesilatele.");
            sb.AppendLine("- Nikdy nezminuj podekování, omluvy ani zdvorilostni vymeny.");
            sb.AppendLine("- Pokud neni zrejmy vysledek, napis co je otevreno nebo co se ceka.");
            sb.AppendLine("- Pokud je to vhodne, preferuj holou informaci pred souvetim.");
            sb.AppendLine("- Pis cesky.");
            sb.AppendLine();
            sb.AppendLine("Priklady spravneho vystupu:");
            sb.AppendLine("SubjectSummary: Nabidka servisni smlouvy – prodlouzeni");
            sb.AppendLine("Summary: Zakaznik pozaduje prodlouzeni servisni smlouvy o rok. Ceka na potvrzeni ceny.");
            sb.AppendLine();
            sb.AppendLine("SubjectSummary: Reklamace faktury");
            sb.AppendLine("Summary: Nesoulad v castce – zakaznik tvrdí, ze zaplatil vice nez je fakturovano. Overujeme u uctarny.");
            sb.AppendLine();
            sb.AppendLine("SubjectSummary: Dotaz na dostupnost zbozi");
            sb.AppendLine("Summary: Dotaz na skladovou dostupnost polozky XY. Informovano: naskladneni do 2 tydnu.");
            sb.AppendLine();
            sb.AppendLine("Zpravy v chronologickem poradi:");

            for (var i = 0; i < messages.Count; i++)
            {
                var msg = messages[i];
                sb.AppendLine($"--- Zprava {i + 1} | {msg.InternalDt:u}");
                sb.AppendLine($"Od: {NullSafe(msg.Sender)}");
                sb.AppendLine($"Predmet: {NullSafe(msg.Subject)}");
                sb.AppendLine("Text:");
                sb.AppendLine(NullSafe(msg.Body));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // -------------------------------------------------------------------------
        // Vyber a normalizace zprav
        // -------------------------------------------------------------------------

        private static List<PromptMessage> BuildNormalizedMessages(List<IMailMessageReference> messages)
        {
            const int maxBodyCharsPerMessage = 360;

            return messages
                .OrderBy(m => m.InternalDt)
                .Select(m => new PromptMessage
                {
                    InternalDt = m.InternalDt,
                    Sender = TrimTo(m.FullContent?.Sender, 120),
                    Subject = TrimTo(m.FullContent?.Subject, 160),
                    Body = TrimTo(ExtractFreshText(m.FullContent?.Content), maxBodyCharsPerMessage)
                })
                .Where(m => !string.IsNullOrWhiteSpace(m.Subject) || !string.IsNullOrWhiteSpace(m.Body))
                .ToList();
        }

        private static List<PromptMessage> BuildPromptMessages(List<PromptMessage> normalized)
        {
            if (normalized.Count <= 1)
                return normalized;

            const int maxMessages = 3;
            const int maxChars = 1800;

            var selected = new List<PromptMessage>();
            var totalChars = 0;

            void addIfPossible(PromptMessage msg, bool force = false)
            {
                if (msg == null || selected.Contains(msg))
                    return;

                var len = EstimateLength(msg);

                if (!force && selected.Count >= maxMessages)
                    return;

                if (!force && (totalChars + len) > maxChars)
                    return;

                selected.Add(msg);
                totalChars += len;
            }

            // Vzdy: prvni zprava (zaklad konverzace)
            addIfPossible(normalized[0], true);

            // Novinka: nejdelsi zprava ze stredu (casto obsahuje jadro – nabidku, odpoved s detaily)
            if (normalized.Count > 2)
            {
                var midCandidate = normalized
                    .Skip(1)
                    .Take(normalized.Count - 2)
                    .OrderByDescending(m => EstimateLength(m))
                    .FirstOrDefault();
                addIfPossible(midCandidate);
            }

            // Nejnovejsi zpravy (aktualni stav)
            for (var i = normalized.Count - 1; i >= 1; i--)
                addIfPossible(normalized[i]);

            return selected.OrderBy(m => m.InternalDt).ToList();
        }

        private static List<PromptMessage> BuildSanitizedMessages(List<PromptMessage> normalizedMessages)
        {
            const int maxSanitizedBodyCharsPerMessage = 360;

            return normalizedMessages
                .Select(m => new PromptMessage
                {
                    InternalDt = m.InternalDt,
                    Sender = TrimTo(SanitizeSender(m.Sender), 120),
                    Subject = TrimTo(SanitizeForAi(m.Subject), 160),
                    Body = TrimTo(SanitizeForAi(m.Body), maxSanitizedBodyCharsPerMessage)
                })
                .ToList();
        }

        private static int EstimateLength(PromptMessage msg)
            => (msg.Sender?.Length ?? 0) + (msg.Subject?.Length ?? 0) + (msg.Body?.Length ?? 0) + 64;

        // -------------------------------------------------------------------------
        // Parsovani odpovedi
        // -------------------------------------------------------------------------

        private static ParsedSummary ParseSummary(string response, string fallbackHint, List<PromptMessage> messages)
        {
            var normalized = (response ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();

            string subject = null;
            string summary = null;

            foreach (var line in lines)
            {
                if (subject == null && line.StartsWith("SubjectSummary:", StringComparison.OrdinalIgnoreCase))
                {
                    subject = line.Substring("SubjectSummary:".Length).Trim();
                    continue;
                }

                if (summary == null && line.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase))
                {
                    summary = line.Substring("Summary:".Length).Trim();
                    continue;
                }

                if (summary != null)
                {
                    if (LooksLikePromptLeak(line))
                        break;

                    summary += " " + line;
                }
            }

            if (string.IsNullOrWhiteSpace(summary))
                summary = TrimTo(normalized.Trim(), 2000);

            summary = StripPromptLeak(summary);

            if (string.IsNullOrWhiteSpace(subject))
                subject = MakeFallbackSubject(fallbackHint, messages.Select(m => m.Subject).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)));

            if (string.IsNullOrWhiteSpace(subject))
                subject = "E-mailova komunikace";

            if (string.IsNullOrWhiteSpace(summary))
                summary = "Nepodarilo se ziskat pouzitelne shrnuti konverzace.";

            return new ParsedSummary
            {
                SubjectSummary = TrimTo(subject, 200),
                Summary = TrimTo(summary, 2000)
            };
        }

        // -------------------------------------------------------------------------
        // Cisteni textu
        // -------------------------------------------------------------------------

        private static string ExtractFreshText(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalized.Split('\n');
            var result = new List<string>(lines.Length);

            foreach (var lineRaw in lines)
            {
                var line = lineRaw?.TrimEnd() ?? string.Empty;
                var trimmed = line.Trim();

                if (IsQuotedReplyBoundary(trimmed))
                    break;

                if (trimmed.StartsWith(">"))
                    continue;

                result.Add(line);
            }

            var joined = string.Join("\n", result);
            joined = TruncateAtInlineQuoteHeaders(joined);
            joined = RemoveSignatureBlock(joined);
            joined = Regex.Replace(joined, @"\n{4,}", "\n\n\n", RegexOptions.CultureInvariant);
            joined = Regex.Replace(joined, @"[ \t]{2,}", " ", RegexOptions.CultureInvariant);

            return joined.Trim();
        }

        private static bool IsQuotedReplyBoundary(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            if (line.StartsWith("From:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Od:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Sent:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Datum:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Date:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("To:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Komu:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Predmet:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Předmět:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("-----Original Message-----", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("---------- Forwarded message ----------", StringComparison.OrdinalIgnoreCase)) return true;
            if (Regex.IsMatch(line, @"^On .+wrote:$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return true;
            if (Regex.IsMatch(line, @"^Dne .+ napsal\(a\):$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return true;

            return false;
        }

        private static string SanitizeSender(string sender)
        {
            var sanitized = SanitizeForAi(sender);
            if (string.IsNullOrWhiteSpace(sanitized))
                return sanitized;

            sanitized = Regex.Replace(sanitized, @"<\s*\[email\]\s*>", "", RegexOptions.CultureInvariant).Trim();
            sanitized = Regex.Replace(sanitized, @"\s{2,}", " ", RegexOptions.CultureInvariant).Trim();

            return string.IsNullOrWhiteSpace(sanitized) ? "[kontakt]" : sanitized;
        }

        private static string SanitizeForAi(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var sanitized = text;

            sanitized = Regex.Replace(sanitized, @"https?://\S+|www\.\S+", "[odkaz]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            sanitized = Regex.Replace(sanitized, @"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b", "[email]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            // Bezna telefonni cisla, kratka obchodni cisla nechavame
            sanitized = Regex.Replace(
                sanitized,
                @"(?<!\w)(?:\+?\d[\d\-\s\(\)]{7,}\d)(?!\w)",
                m => CountDigits(m.Value) >= 8 ? "[telefon]" : m.Value,
                RegexOptions.CultureInvariant);

            // Delsi numericke identifikatory (cisla objednavek, uctu, sledovaci cisla)
            sanitized = Regex.Replace(
                sanitized,
                @"(?<!\w)\d{6,}(?!\w)",
                "[id]",
                RegexOptions.CultureInvariant);

            sanitized = Regex.Replace(sanitized, @"\s{2,}", " ", RegexOptions.CultureInvariant).Trim();

            return sanitized;
        }

        private static string TruncateAtInlineQuoteHeaders(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var matches = new List<int>();

            void addMatch(string pattern)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (match.Success && match.Index > 40)
                    matches.Add(match.Index);
            }

            addMatch(@"(?:^|\s)Od\s+[""<]");
            addMatch(@"(?:^|\s)From:\s");
            addMatch(@"(?:^|\s)Komu:\s");
            addMatch(@"(?:^|\s)To:\s");
            addMatch(@"(?:^|\s)Datum:\s");
            addMatch(@"(?:^|\s)Date:\s");
            addMatch(@"(?:^|\s)Předmět:\s");
            addMatch(@"(?:^|\s)Predmet:\s");
            addMatch(@"(?:^|\s)Subject:\s");

            if (matches.Count == 0)
                return text;

            return text.Substring(0, matches.Min()).TrimEnd();
        }

        private static string RemoveSignatureBlock(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var markers = new[]
            {
                "\nS pozdravem",
                "\nSe srdecnym pozdravem",
                "\nS pranim hezkeho dne",
                "\nDekuji a preji hezky den",
                "\nDekuji a preji krasny den",
                "\nBest regards",
                "\nKind regards",
                "\nRegards,"
            };

            var result = text;
            foreach (var marker in markers)
            {
                var idx = result.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx > 40)
                {
                    result = result.Substring(0, idx).TrimEnd();
                    break;
                }
            }

            return result;
        }

        // -------------------------------------------------------------------------
        // Detekce a cisteni prompt leaku v odpovedi
        // -------------------------------------------------------------------------

        private static bool LooksLikePromptLeak(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            if (line.StartsWith("---")) return true;
            if (line.StartsWith("Zprava ", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Zpráva ", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Od:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Predmet:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Předmět:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Text:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("SubjectSummary:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private static string StripPromptLeak(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
                return summary;

            var leakMarkers = new[]
            {
                "--- Zprava",
                "--- Zpráva",
                " Zprava ",
                " Zpráva ",
                " Od: ",
                " Predmet: ",
                " Předmět: ",
                " Text: "
            };

            var result = summary;
            foreach (var marker in leakMarkers)
            {
                var idx = result.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    result = result.Substring(0, idx).Trim();
            }

            return StripAssistantBoilerplate(result);
        }

        private static string StripAssistantBoilerplate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var result = text.Trim();
            var patterns = new[]
            {
                @"\s*---\s*$",
                @"\s*Pokud potrebujete vice informaci.*$",
                @"\s*Kdybyste potrebovali vice informaci.*$",
                @"\s*Dejte mi vedet, pokud.*$",
                @"\s*Pokud chcete, mohu.*$"
            };

            foreach (var pattern in patterns)
                result = Regex.Replace(result, pattern, "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            return result.Trim();
        }

        // -------------------------------------------------------------------------
        // Pomocne metody
        // -------------------------------------------------------------------------

        private static string BuildDirectSummary(List<PromptMessage> messages)
        {
            var sb = new StringBuilder();

            foreach (var message in messages)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                var sender = string.IsNullOrWhiteSpace(message.Sender) ? "Neznamy odesilatel" : message.Sender.Trim();
                var text = message.Body;

                if (string.IsNullOrWhiteSpace(text))
                    text = message.Subject;

                text = TrimTo(text, 500);

                if (string.IsNullOrWhiteSpace(text))
                    sb.Append(sender).Append(":");
                else
                    sb.Append(sender).Append(": ").Append(text);
            }

            return sb.ToString().Trim();
        }

        private static string MakeFallbackSubject(params string[] candidates)
        {
            var candidate = candidates?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
            if (string.IsNullOrWhiteSpace(candidate))
                return "E-mailova komunikace";

            var cleaned = candidate.Trim();
            while (!string.IsNullOrWhiteSpace(cleaned))
            {
                var updated = Regex.Replace(cleaned, @"^\s*(?:(?:re|fw|fwd)\s*:\s*)+", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
                if (string.Equals(updated, cleaned, StringComparison.Ordinal))
                    break;

                cleaned = updated;
            }

            return TrimTo(string.IsNullOrWhiteSpace(cleaned) ? "E-mailova komunikace" : cleaned, 200);
        }

        private static string TrimTo(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }

        private static string NullSafe(string value)
            => string.IsNullOrWhiteSpace(value) ? "-" : value;

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return Regex.Matches(text, @"\S+", RegexOptions.CultureInvariant).Count;
        }

        private static int CountDigits(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            var count = 0;
            foreach (var ch in text)
                if (char.IsDigit(ch))
                    count++;

            return count;
        }

        private sealed class PromptMessage
        {
            public DateTime InternalDt { get; set; }
            public string Sender { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }
        }

        private sealed class ParsedSummary
        {
            public string SubjectSummary { get; set; }
            public string Summary { get; set; }
        }
    }
}
