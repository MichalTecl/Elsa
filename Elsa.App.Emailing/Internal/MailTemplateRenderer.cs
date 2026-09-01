using System;
using System.Collections.Generic;
using System.Linq;
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
            var caseInsensitiveValues = ToCaseInsensitiveDictionary(values);
            var subject = ReplaceKnownPlaceholders(template.Subject, caseInsensitiveValues);
            var body = ReplaceKnownPlaceholders(template.Body, caseInsensitiveValues);

            var unresolvedPlaceholders = _placeholderRegex.Matches(subject)
                .Cast<Match>()
                .Concat(_placeholderRegex.Matches(body).Cast<Match>())
                .Select(match => match.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unresolvedPlaceholders.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Pro e-mailovou šablonu '{templateTypeName}' nebyly vyplněny placeholdery: {string.Join(", ", unresolvedPlaceholders)}.");
            }

            return new MailTemplateContent(subject, body);
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

        private static string ReplaceKnownPlaceholders(string value, IReadOnlyDictionary<string, string> values)
        {
            return _placeholderRegex.Replace(value ?? string.Empty, match =>
            {
                var placeholderName = match.Groups["name"].Value;
                return values.TryGetValue(placeholderName, out var replacement)
                    ? replacement
                    : match.Value;
            });
        }
    }
}
