using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.CommonData.ExcelInterop
{
    public class SetUnitByMaterialAttribute : R1C1FormulaAttribute
    {
        public SetUnitByMaterialAttribute(string materialNameR1C1) : base($"=VLOOKUP({materialNameR1C1}, DATA_Materials!A1:B9999, 2, FALSE)")
        {
        }
    }
}
