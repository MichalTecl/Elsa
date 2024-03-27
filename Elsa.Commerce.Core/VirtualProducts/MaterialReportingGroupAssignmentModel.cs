using XlsSerializer.Core.Attributes;

namespace Elsa.Commerce.Core.VirtualProducts
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class MaterialReportingGroupAssignmentModel
    {
        [XlsColumn("A", "Materiál v Else", "@")]
        public string MaterialName { get; set; }

        [XlsColumn("B", "Skupina", "@")]
        public string ReportingGroupName { get; set; }
    }
}
