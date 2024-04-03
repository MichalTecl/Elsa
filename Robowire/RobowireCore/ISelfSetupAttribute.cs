using System;

namespace Robowire
{
    public interface ISelfSetupAttribute
    {
        void Setup(Type markedType, IContainerSetup setup);
    }
}
