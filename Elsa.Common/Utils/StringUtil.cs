using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Elsa.Common.Utils
{
    public static class StringUtil
    {        
        private static readonly Dictionary<char, char> s_searchStringReplacements = new Dictionary<char, char>()
                                                                            {
                                                                                    { 'á', 'a' },
                                                                                    { 'č', 'c' },
                                                                                    { 'ď', 'd' },
                                                                                    { 'ě', 'e' },
                                                                                    { 'é', 'e' },
                                                                                    { 'í', 'i' },
                                                                                    { 'ň', 'n' },
                                                                                    { 'ó', 'o' },
                                                                                    { 'ř', 'r' },
                                                                                    { 'š', 's' },
                                                                                    { 'ť', 't' },
                                                                                    { 'ú', 'u' },
                                                                                    { 'ů', 'u' },
                                                                                    { 'w', 'v' },
                                                                                    { 'y', 'i' },
                                                                                    { '0', 'o' }
                                                                            };

        internal static string AddNumberToFileName(string path, int number)
        {
            var directory = Path.GetDirectoryName(path);
            var extension = Path.GetExtension(path);
            var fn = Path.GetFileNameWithoutExtension(path);

            fn = $"{fn}_{number}{extension}";

            return Path.Combine(directory, fn);
        }

        private static readonly Dictionary<char, char> s_seoStringReplacements = new Dictionary<char, char>()
                                                                                     {
                                                                                             { 'á', 'a' },
                                                                                             { 'č', 'c' },
                                                                                             { 'ď', 'd' },
                                                                                             { 'ě', 'e' },
                                                                                             { 'é', 'e' },
                                                                                             { 'í', 'i' },
                                                                                             { 'ň', 'n' },
                                                                                             { 'ó', 'o' },
                                                                                             { 'ř', 'r' },
                                                                                             { 'š', 's' },
                                                                                             { 'ť', 't' },
                                                                                             { 'ú', 'u' },
                                                                                             { 'ů', 'u' }
                                                                                     };
                                                                                    



        private const string c_searchStringValidChars = "abcdefghijklmnopqrstuvxz123456789";
        private const string c_seoValidChars = "abcdefghijklmnopqrstuvwxyz1234567890";

        public static string RemoveNumericChars(string inp)
        {
            if (string.IsNullOrWhiteSpace(inp))
            {
                return inp;
            }

            var sb = new StringBuilder(inp.Length);
            foreach (var ch in inp)
            {
                if (!char.IsDigit(ch))
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        public static string NormalizeSearchText(int lenLimit, IEnumerable<string> inp)
        {
            if (inp == null)
            {
                return string.Empty;
            }


            var sb = new StringBuilder();

            char lChar = '#';

            foreach (var s in inp.Where(i => !string.IsNullOrWhiteSpace(i)))
            {
                foreach (var chr in s.Trim().ToLowerInvariant())
                {
                    char nChar;
                    if (!s_searchStringReplacements.TryGetValue(chr, out nChar))
                    {
                        nChar = chr;
                    }

                    if (!c_searchStringValidChars.Contains(nChar) || (((nChar == lChar) && !char.IsDigit(nChar))))
                    {
                        continue;
                    }

                    lChar = nChar;
                    sb.Append(nChar);
                    if (sb.Length == lenLimit)
                    {
                        return sb.ToString();
                    }
                }
            }

            return sb.ToString();
        }

        public static string NormalizeSearchText(int lenLimit, params string[] inp)
        {
            return NormalizeSearchText(lenLimit, inp.ToList());
        }

        public static string Nvl(params string[] s)
        {
            foreach (var i in s)
            {
                if (!string.IsNullOrWhiteSpace(i))
                {
                    return i;
                }
            }

            return s.LastOrDefault();
        }

        public static string FormatDecimal(decimal n)
        {
            var s = n.ToString(CultureInfo.InvariantCulture);

            if (s.Contains(".") || s.Contains(","))
            {
                s = s.TrimEnd('0');
            }
            
            return s.TrimEnd('.');
        }

        public static string FormatDecimal(decimal? n)
        {
            if (n == null)
            {
                return null;
            }

            return FormatDecimal(n.Value);
        }

        public static string FormatDecimal(decimal n, CultureInfo culture)
        {
            var s = n.ToString(culture);

            if (s.Contains(".") || s.Contains(","))
            {
                s = s.TrimEnd('0');
            }

            return s.TrimEnd(culture.NumberFormat.CurrencyDecimalSeparator[0]);
        }

        public static string FormatPrice(decimal price)
        {
            return price.ToString("F");
        }

        public static string FormatPrice(decimal price, CultureInfo culture)
        {
            return price.ToString("F", culture);
        }

        public static string Display(this decimal value, string unit = null)
        {
            if (unit == null)
            {
                return FormatDecimal(value);
            }

            return $"{FormatDecimal(value)}{unit}";
        }

        public static string Limit(this string inp, int len, string rightSideCutMarker = "")
        {
            if (string.IsNullOrEmpty(inp))
            {
                return inp;
            }

            if (inp.Length > len)
            {
                return string.Concat(inp.Substring(0, len - rightSideCutMarker.Length), rightSideCutMarker);
            }

            return inp;
        }

        public static string FormatDateTime(DateTime dt)
        {
            return dt.ToString("dd.MM.yy HH:mm");
        }

        public static string FormatDate(DateTime dt)
        {
            return dt.ToString("dd.MM.yyyy");
        }

        public static string FormatDate(DateTime? dt, string defaultValue = null)
        {
            if (dt == null) 
                return defaultValue;

            return FormatDate(dt.Value);
        }

        public static DateTime ParseDateTime(string modelDisplayDt)
        {
            return DateTime.ParseExact(modelDisplayDt, "dd.MM.yy HH:mm", CultureInfo.CurrentCulture);
        }

        public static float GetReadability(decimal number)
        {
            var strNumber = FormatDecimal(number);

            return 1f / ((float)strNumber.Length);
        }

        public static string ReplaceNationalChars(string inp)
        {
            foreach (var r in s_seoStringReplacements)
            {
                inp = inp.Replace(r.Key, r.Value);
            }
            
            return inp;
        }

        public static string ConvertToBaseText(string inp, char whitespaceReplacement, char invalidCharReplacement, int wordLenLimit = 1000)
        {
            inp = inp.ToLowerInvariant();
            
            var sb = new StringBuilder();
            var lastChar = '*';

            var currentWordLength = 0;
            foreach (var chr in inp.ToCharArray())
            {
                char replChr;
                if (!s_seoStringReplacements.TryGetValue(chr, out replChr))
                {
                    replChr = chr;
                    
                    if (char.IsWhiteSpace(replChr))
                    {
                        replChr = whitespaceReplacement;
                    }

                    if (!c_seoValidChars.Contains(replChr))
                    {
                        replChr = invalidCharReplacement;
                    }
                }

                if (lastChar == replChr)
                {
                    continue;
                }
                lastChar = replChr;

                if (replChr == whitespaceReplacement)
                {
                    currentWordLength = 0;
                }

                if (currentWordLength <= wordLenLimit)
                {
                    sb.Append(replChr);
                    currentWordLength++;
                }
            }

            return sb.ToString();
        }

        public static string JoinUrlSegments(params string[] segments)
        {
            return string.Join("/",
                segments.Select(s =>
                {
                    s = s.Trim();

                    while (s.StartsWith("/"))
                    {
                        s = s.Substring(1);
                    }

                    while (s.EndsWith("/"))
                    {
                        s = s.Substring(0, s.Length - 1);
                    }

                    return s;
                }));
        }

        public static string ToFileName(string a)
        {
            if (string.IsNullOrWhiteSpace(a))
            {
                return "BezNazvu";
            }

            a = a.Trim();

            return string.Join(string.Empty, Underscorise(a));
        }

        private static IEnumerable<char> Underscorise(string inp)
        {
            var lastWasUnderscore = false;

            foreach (var ch in inp)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    yield return ch;
                    lastWasUnderscore = false;
                }
                else if (!lastWasUnderscore)
                {
                    yield return '_';
                    lastWasUnderscore = true;
                }
            }
        }

        public static string SanitizeFileName(string inp, char replacer = '_')
        {
            if (string.IsNullOrWhiteSpace(inp))
            {
                return "BezNazvu";
            }

            inp = string.Join(replacer.ToString(),
                inp.Trim().Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

            var doubleReplacer = $"{replacer}{replacer}";

            while (inp.Contains(doubleReplacer))
            {
                inp = inp.Replace(doubleReplacer, replacer.ToString());
            }

            inp = inp.Trim(replacer);

            if (string.IsNullOrWhiteSpace(inp))
            {
                inp = "BezNazvu";
            }

            return inp;
        }

        public static bool MatchStarWildcard(string pattern, string input)
        {
            pattern = pattern?.ToLowerInvariant() ?? string.Empty;
            input = input?.ToLowerInvariant() ?? string.Empty;

            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*") + "$";
            return Regex.IsMatch(input, regexPattern);
        }

        public static string GetHash(string input) 
        {
            var oData = Encoding.UTF8.GetBytes(input);

            using (var md5 = new MD5CryptoServiceProvider())
            {
                var hash = md5.ComputeHash(oData);
                return Convert.ToBase64String(hash);
            }
        }

        public static int? TryParseInt(string input, Func<string, int?> handleFailedParse = null)
        {
            if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out var result))
                return result;

            return handleFailedParse == null ? null : handleFailedParse(input);
        }
    }
}
