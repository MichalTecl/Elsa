using System.Web.Routing;

namespace Robowire.RoboApi.Internal
{
    public interface IControllerNameExtractor
    {
        string GetControllerName(RequestContext requestContext, string defaultControllerName);
    }
}
