using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.Common.ViewModels
{
    public class ReportRow
    {
        public string RowId { get; set; }

        public List<string> Values { get; set; } = new List<string>();
    }
}
