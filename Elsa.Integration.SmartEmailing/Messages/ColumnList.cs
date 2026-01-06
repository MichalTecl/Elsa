using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.SmartEmailing.Messages
{
    public class ColumnList
    {
        private readonly string[] _columns;

        private ColumnList(string[] columns)
        {
            _columns = columns;
        }

        public static ColumnList Set(params string[] columns)
        {
            return new ColumnList(columns);
        }

        public override string ToString()
        {
            return string.Join(",", _columns.Select(c => $"{c}"));
        }
    }
}
