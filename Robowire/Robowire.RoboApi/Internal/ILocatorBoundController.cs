using System.Web.Mvc;

namespace Robowire.RoboApi.Internal
{
    public interface ILocatorBoundController : IController
    {
        IServiceLocator Locator { get; }
    }
}
