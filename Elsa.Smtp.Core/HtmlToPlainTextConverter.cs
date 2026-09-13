using System.Net;
using System.Text.RegularExpressions;

namespace Elsa.Smtp.Core
{
    public static class HtmlToPlainTextConverter
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

        public static string Convert(string html)
        {
            var withoutNonContent = _nonContentHtmlRegex.Replace(html ?? string.Empty, string.Empty);
            var withLineBreaks = _htmlLineBreakRegex.Replace(withoutNonContent, "\r\n");
            var withoutTags = _htmlTagRegex.Replace(withLineBreaks, string.Empty);
            var decoded = WebUtility.HtmlDecode(withoutTags);
            return _extraLineBreakRegex.Replace(decoded, "\r\n\r\n").Trim();
        }
    }
}
