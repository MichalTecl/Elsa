using Robowire;
using System;

namespace Elsa.App.PublicFiles
{
    public class PublicFilesRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IPublicFilesHelper>().Use<Impl.PublicFiles>();
        }
    }
}
