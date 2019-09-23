using XlsSerializer.Core.Attributes;

namespace Elsa.Apps.CommonData.ExcelInterop
{
    public class ValidateUnitAttribute : ListValidationAttribute
    {
        public ValidateUnitAttribute() : base("DATA_Units!A:A")
        {
        }
    }
}
