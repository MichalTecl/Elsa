using System;
using System.Reflection;
using System.Web.Routing;

using Robowire.RoboApi.Extensibility;

namespace Robowire.RoboApi.Convention.Default
{
    internal sealed class DefaultInterceptor : IControllerInterceptor
    {
        public bool OnRequest(object controller, RequestContext request)
        {
            return false;
        }

        public T ObtainParameterValue<T>(
            object controller,
            MethodInfo method,
            ParameterInfo parameter,
            RequestContext context,
            Func<T> defaultValueFactory)
        {
            return defaultValueFactory();
        }

        public void Call(
            object controller,
            MethodInfo method,
            RequestContext context,
            object[] parameters,
            bool isVoid,
            Func<object> defaultInvocation,
            Action<object> defaultResultWrite)
        {
            defaultResultWrite(defaultInvocation());
        }

        public void OnException(object controller, MethodInfo method, RequestContext context, object[] parameters, Exception exception)
        {
            throw exception;
        }
    }
}
