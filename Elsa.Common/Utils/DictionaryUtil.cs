using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Utils
{
    public static class DictionaryUtil
    {
        public static string GetOrDefault(this IDictionary<string, string> dict, string key, string defaultValue = null)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }

            key = dict.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
            return key == null ? defaultValue : dict[key];
        }
    }
}
