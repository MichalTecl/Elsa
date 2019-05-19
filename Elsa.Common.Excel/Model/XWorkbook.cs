using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.XTable.Model
{
    public class XWorkbook
    {
        public List<XSheet> Sheets { get; set; } = new List<XSheet>();
    }
}
