using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.CommonData.ExcelInterop
{
    public class MaterialAndUnit
    {
        [XlsColumn(0)]
        public string MaterialName { get; set; }

        [XlsColumn(1)]
        public string Unit { get; set; }
    }
}
