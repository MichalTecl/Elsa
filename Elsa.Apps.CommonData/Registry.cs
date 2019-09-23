using Elsa.Apps.CommonData.ExcelInterop;
using Robowire;

namespace Elsa.Apps.CommonData
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<ElsaExcelModelFactory>().Use<ElsaExcelModelFactory>();
        }
    }
}
