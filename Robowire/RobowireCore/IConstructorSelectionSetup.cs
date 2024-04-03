using System;

namespace Robowire
{
    public interface IConstructorSelectionSetup
    {
        IDependencyResolutionSetup Constructor(params Type[] argumentType);
    }
}
