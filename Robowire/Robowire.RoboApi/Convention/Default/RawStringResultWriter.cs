using System.Reflection;
using System.Web.Routing;

namespace Robowire.RoboApi.Convention.Default
{
    public sealed class RawStringResultWriter : IResultWriter
    {
        public void WriteResult(MethodInfo controllerMethod, RequestContext context, object returnValue, bool isVoid)
        {
            var strVal = returnValue?.ToString();

            if (strVal != null)
            {
                context.HttpContext.Response.Write(strVal);
            }
        }
    }
}
