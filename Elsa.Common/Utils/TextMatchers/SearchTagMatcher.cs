using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Utils.TextMatchers
{
    public class SearchTagMatcher : ITextMatcher
    {
        private readonly string[] _words;

        private SearchTagMatcher(string[] words)
        {
            _words = words;
        }

        public bool Match(string text)
        {
            foreach (var w in _words)
                if (text.IndexOf(w) == -1)
                    return false;

            return true;
        }

        public static ITextMatcher GetMatcher(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return AllMatchingMatcher.Instance;

            var words = query.Split(' ').Select(w => StringUtil.NormalizeSearchText(100, w)).Where(w => !string.IsNullOrWhiteSpace(w)).OrderByDescending(w => w.Length).ToArray();

            return words.Length > 0 ? new SearchTagMatcher(words) : AllMatchingMatcher.Instance;
        }
    }
}
