using System;

namespace Robowire
{
    public interface IDependencyResolutionSetup
    {
        IDependencyResolutionSetup With<T>(T value);

        IDependencyResolutionSetup With<T>(Func<IServiceLocator, T> valueFactory);

        IDependencyResolutionSetup With<T>(string paramName, T value);

        IDependencyResolutionSetup With<T>(string paramName, Func<IServiceLocator, T> valueFactory);
    }
}
