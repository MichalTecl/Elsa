using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Integration.Geocoding.Common;

using Robowire;

namespace Elsa.Integration.Geocoding.OpenStreetMap
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IGeocodingProvider>().Use<OpenStreetMapClient>();
        }
    }
}
