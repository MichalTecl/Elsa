using System.Reflection;
using System.Web.Routing;

namespace Robowire.RoboApi.Convention
{
    public interface IParameterReader
    {
        T Read<T>(ParameterInfo parameter, RequestContext context);
    }
    
}
