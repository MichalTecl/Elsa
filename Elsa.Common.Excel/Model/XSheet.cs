using System.Collections.Generic;

namespace Elsa.Common.XTable.Model
{
    public class XSheet : XElementBase
    {
        public List<XRow> Rows { get; set; } = new List<XRow>();
    }
}
