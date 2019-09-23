using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.CommonData.ExcelInterop
{
    public class ValidateMaterialAttribute : ListValidationAttribute
    {
        public ValidateMaterialAttribute() : base("DATA_Materials!A:A")
        {
            AllowBlank = false;
            base.Error = "Neplatný název materiálu";
        }
    }
}
