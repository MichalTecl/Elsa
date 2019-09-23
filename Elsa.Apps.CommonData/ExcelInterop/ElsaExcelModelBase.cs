using System.Collections.Generic;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.CommonData.ExcelInterop
{
    public class ElsaExcelModelBase
    {
        public const string ExcelDateFormat = "dd.mm.yyyy";

        [XlsSheet(20, "DATA_Suppliers", IsHidden = true)]
        public List<SupplierAndCurrency> Suppliers { get;  } = new List<SupplierAndCurrency>();

        [XlsSheet(30, "DATA_Materials", IsHidden = true)]
        public List<MaterialAndUnit> Materials { get; } = new List<MaterialAndUnit>();

        [XlsSheet(40, "DATA_Currencies", IsHidden = true)]
        public List<string> Currencies { get; } = new List<string>();

        [XlsSheet(50, "DATA_Units", IsHidden = true)]
        public List<string> Units { get; } = new List<string>();
    }
}
