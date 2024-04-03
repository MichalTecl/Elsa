using System;

namespace Robowire.Core
{
    public class NamedFactory
    {
        public NamedFactory(Func<IServiceLocator, object> factory)
        {
            Name = $"f{Guid.NewGuid():N}";
            Factory = factory;
        }

        public string Name { get; }

        public Func<IServiceLocator, object> Factory { get; }
    }
}
