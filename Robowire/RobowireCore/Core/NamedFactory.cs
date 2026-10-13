using System;

using CodeGeneration;

namespace Robowire.Core
{
    public class NamedFactory
    {
        public NamedFactory(Func<IServiceLocator, object> factory)
        {
            Name = $"f{UniqueNames.Next()}";
            Factory = factory;
        }

        public string Name { get; }

        public Func<IServiceLocator, object> Factory { get; }
    }
}
