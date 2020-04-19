using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.App.MaterialLevels.Components;
using Robowire;

namespace Elsa.App.MaterialLevels
{
    public class MaterialLevelsRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IMaterialLevelsLoader>().Use<MaterialLevelsLoader>();
            setup.For<IInventoryWatchRepository>().Use<InventoryWatchRepository>();
        }
    }
}
