using System.Reflection;
using System.Web.Routing;

namespace Robowire.RoboApi.Convention
{
    public interface IResultWriter
    {
        void WriteResult(MethodInfo controllerMethod, RequestContext context, object returnValue, bool isVoid);
    }
}
