using System;
using System.Reflection;
using System.Web.Routing;

namespace Robowire.RoboApi.Extensibility
{
    public interface IControllerInterceptor
    {
        bool OnRequest(object controller, RequestContext request);
        
        T ObtainParameterValue<T>(
            object controller,
            MethodInfo method,
            ParameterInfo parameter,
            RequestContext context,
            Func<T> defaultValueFactory);

        void Call(
            object controller,
            MethodInfo method,
            RequestContext context,
            object[] parameters,
            bool isVoid,
            Func<object> defaultInvocation,
            Action<object> defaultResultWrite);
        
        void OnException(
            object controller,
            MethodInfo method,
            RequestContext context,
            object[] parameters,
            Exception exception);
    }

}
