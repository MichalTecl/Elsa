using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.CommonData.ExcelInterop
{
    public class SupplierAndCurrency
    {
        [XlsColumn(0)]
        public string SupplierName { get; set; }

        [XlsColumn(1)]
        public string Currency { get; set; }
    }
}
