using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Elsa.Common.Utils
{
    public static class StringUtil
    {
        private static readonly Dictionary<char, char> s_replacements = new Dictionary<char, char>()
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

        private const string c_validChars = "abcdefghijklmnopqrstuvxz123456789";

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
                    if (!s_replacements.TryGetValue(chr, out nChar))
                    {
                        nChar = chr;
                    }

                    if (!c_validChars.Contains(nChar) || ((nChar == lChar && !char.IsDigit(nChar))))
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

        public static string Limit(string inp, int len)
        {
            if (string.IsNullOrEmpty(inp))
            {
                return inp;
            }

            if (inp.Length > len)
            {
                return inp.Substring(0, len);
            }

            return inp;
        }

        public static string FormatDateTime(DateTime dt)
        {
            return dt.ToString("dd.MM.yy HH:mm");
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
            foreach (var r in s_replacements)
            {
                inp = inp.Replace(r.Key, r.Value);
            }
            
            return inp;
        }

        public static string ConvertToBaseText(string inp, char whitespaceReplacement, char invalidCharReplacement)
        {
            inp = inp.ToLowerInvariant();

            var sb = new StringBuilder();
            var lastChar = '*';

            foreach (var chr in inp.ToCharArray())
            {
                char replChr;
                if (!s_replacements.TryGetValue(chr, out replChr))
                {
                    replChr = chr;
                    
                    if (char.IsWhiteSpace(replChr))
                    {
                        replChr = whitespaceReplacement;
                    }

                    if (!c_validChars.Contains(replChr))
                    {
                        replChr = invalidCharReplacement;
                    }
                }

                if (lastChar == replChr)
                {
                    continue;
                }
                lastChar = replChr;
                sb.Append(replChr);
            }

            return sb.ToString();
        }
    }
}
