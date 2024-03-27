using Robowire;
using System.ComponentModel;

namespace Elsa.App.ImportExport
{
    public class ImportExportRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.Collect<IImportExportModule>();
        }
    }
}
