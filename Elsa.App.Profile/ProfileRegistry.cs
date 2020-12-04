using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robowire;

namespace Elsa.App.Profile
{
    public class ProfileRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            //setup.ScanAssembly(typeof(ProfileController).Assembly);
        }
    }
}
