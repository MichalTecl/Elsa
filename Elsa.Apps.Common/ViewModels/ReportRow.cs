using System.Collections.Generic;

namespace Elsa.Apps.Common.ViewModels
{
    public class ReportRow
    {
        public string RowId { get; set; }

        public List<string> Values { get; set; } = new List<string>();
    }
}
