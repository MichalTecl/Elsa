using Robowire;

namespace Elsa.Jobs.BulletinGeneration
{
    public class BulletinGenerationRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<BulletinGenerationJob>().Use<BulletinGenerationJob>();
            setup.For<BulletinDocumentGenerator>().Use<BulletinDocumentGenerator>();
        }
    }
}
