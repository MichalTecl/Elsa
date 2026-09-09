using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

using Elsa.Smtp.Core;

namespace Elsa.App.Emailing.Internal
{
    public class MailTemplateRenderer : IMailTemplateRenderer
    {
        private static readonly Regex _placeholderRegex = new Regex(
            @"\{%(?<name>[^{}%]+)%\}",
            RegexOptions.Compiled);

        private readonly IMailTemplateRepository _repository;

        public MailTemplateRenderer(IMailTemplateRepository repository)
        {
            _repository = repository;
        }

        public MailTemplateContent Render(string templateTypeName, Dictionary<string, string> values)
        {
            var template = _repository.GetByTypeName(templateTypeName);
            return RenderContent(
                template.Subject,
                template.Body,
                template.BodyFormat,
                values,
                $"e-mailovou šablonu '{templateTypeName}'");
        }

        public MailTemplateContent RenderContent(
            string subject,
            string body,
            string bodyFormat,
            Dictionary<string, string> values)
        {
            return RenderContent(subject, body, bodyFormat, values, "testovanou šablonu");
        }

        private static MailTemplateContent RenderContent(
            string subject,
            string body,
            string bodyFormat,
            Dictionary<string, string> values,
            string templateDescription)
        {
            var normalizedBodyFormat = bodyFormat ?? MailTemplateBodyFormats.PlainText;
            if (normalizedBodyFormat != MailTemplateBodyFormats.PlainText
                && normalizedBodyFormat != MailTemplateBodyFormats.Html)
            {
                throw new InvalidOperationException("Vybraný formát těla e-mailu není podporovaný.");
            }

            var caseInsensitiveValues = ToCaseInsensitiveDictionary(values);
            var renderedSubject = ReplaceKnownPlaceholders(subject, caseInsensitiveValues);
            var renderedBody = ReplaceKnownPlaceholders(
                body,
                caseInsensitiveValues,
                normalizedBodyFormat == MailTemplateBodyFormats.Html);

            var unresolvedPlaceholders = _placeholderRegex.Matches(renderedSubject)
                .Cast<Match>()
                .Concat(_placeholderRegex.Matches(renderedBody).Cast<Match>())
                .Select(match => match.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unresolvedPlaceholders.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Pro {templateDescription} nebyly vyplněny placeholdery: {string.Join(", ", unresolvedPlaceholders)}.");
            }

            return new MailTemplateContent(
                renderedSubject,
                renderedBody,
                normalizedBodyFormat == MailTemplateBodyFormats.Html);
        }

        private static Dictionary<string, string> ToCaseInsensitiveDictionary(Dictionary<string, string> values)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in values ?? new Dictionary<string, string>())
            {
                if (pair.Key == null)
                {
                    throw new InvalidOperationException("Název placeholderu nesmí být null.");
                }

                result[pair.Key] = pair.Value ?? string.Empty;
            }

            return result;
        }

        private static string ReplaceKnownPlaceholders(
            string value,
            IReadOnlyDictionary<string, string> values,
            bool htmlEncode = false)
        {
            return _placeholderRegex.Replace(value ?? string.Empty, match =>
            {
                var placeholderName = match.Groups["name"].Value;
                if (!values.TryGetValue(placeholderName, out var replacement))
                {
                    return match.Value;
                }

                return htmlEncode ? WebUtility.HtmlEncode(replacement) : replacement;
            });
        }
    }
}
