using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Utils
{
    public class CsvList<T> : List<T>
    {
        private readonly string _separator;
        private readonly Func<T, string> _valueToString;
        private readonly Func<string, T> _parseValue;

        public CsvList(Func<T, string> valueToString, Func<string, T> parseValue, string separator = ",")
        {
            _valueToString = valueToString;
            _parseValue = parseValue;
            _separator = separator;
        }

        [JsonIgnore]
        public string Csv
        {
            get
            {
                return string.Join(",", this.Select(v => _valueToString(v)));
            }

            set
            {
                Clear();
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                AddRange(
                    value.Split(new[] { _separator }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(_parseValue));
            }
        }
    }

    public class IntCsvList : CsvList<int>
    {
        public IntCsvList() : base(v => v.ToString(), v => int.Parse(v))
        {
        }
    }

}
