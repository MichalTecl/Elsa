using System.Collections.Generic;

namespace Elsa.Common.XTable.Model
{
    public class XRow : XElementBase
    {
        public List<XCell> Cells { get; set;} = new List<XCell>();
    }
}
