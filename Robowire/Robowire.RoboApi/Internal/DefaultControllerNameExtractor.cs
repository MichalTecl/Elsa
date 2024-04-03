using System.Web.Routing;

namespace Robowire.RoboApi.Internal
{
    public class DefaultControllerNameExtractor : IControllerNameExtractor
    {
        public string GetControllerName(RequestContext requestContext, string defaultControllerName)
        {
            return defaultControllerName;
        }
    }
}
