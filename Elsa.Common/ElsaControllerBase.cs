using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Routing;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Newtonsoft.Json;
using Robowire.RoboApi.Extensibility;

namespace Elsa.Common
{
    public abstract class ElsaControllerBase : IControllerInterceptor
    {
        private readonly ILog m_log;

        protected ElsaControllerBase(IWebSession webSession, ILog log)
        {
            WebSession = webSession;
            m_log = log;
        }

        protected IWebSession WebSession { get; }

        protected ILog Log => m_log;

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
            if (WebSession.User == null)
            {
                if (!Attribute.IsDefined(method, typeof(AllowAnonymousAttribute)))
                {
                    throw new UnauthorizedAccessException("Requested resource is not accessible to anonymous user");
                }
            }
            else if (WebSession.User.UsesDefaultPassword)
            {
                if (!method.GetCustomAttributes().OfType<AllowWithDefaultPassword>().Any())
                {
                    throw new UnauthorizedAccessException("Requested resource is not accessible to users with default password");
                }
            }

            if (typeof(ICustomResult).IsAssignableFrom(method.ReturnType))
            {
                defaultResultWrite = r =>
                    {
                        ((ICustomResult)r).WriteResponse(context.HttpContext);
                    };
            }

            if (ShouldBeLogged(method))
            {
                using (
                    m_log.StartStopwatch(
                        $"{method.DeclaringType?.Name}.{method.Name}({GetParamString(method, parameters)})"))
                {
                    defaultResultWrite(defaultInvocation());
                }
            }
            else
            {
                defaultResultWrite(defaultInvocation());
            }
        }

        public void OnException(object controller, MethodInfo method, RequestContext context, object[] parameters, Exception exception)
        {
            var paramString = GetParamString(method, parameters);
            m_log.Error($"{GetType().Name}.{method.Name}({paramString}) failed: \"{exception?.Message}\"", exception);

            context.HttpContext.Response.StatusDescription = exception?.Message;
            
            if (exception is UnauthorizedAccessException)
            {
                context.HttpContext.Response.StatusCode = 401;
            }

            var errorObj = new ErrorResponseModel(500, exception?.Message ?? "Došlo k chybě");

            context.HttpContext.Response.ClearContent();
            context.HttpContext.Response.Write(JsonConvert.SerializeObject(errorObj));
        }

        private static string GetParamString(MethodInfo method, object[] parameters)
        {
            if (Attribute.IsDefined(method, typeof(DoNotLogParamsAttribute)))
            {
                return "?";
            }

            return string.Join(
                ", ",
                parameters.Select(
                    p =>
                        {
                            if (p == null)
                            {
                                return "null";
                            }

                            var sValue = p.ToString();
                            if (sValue.Length > 10)
                            {
                                sValue = $"{sValue.Substring(0, 10)}...";
                            }

                            return sValue;
                        }));
        }

        private static bool ShouldBeLogged(MethodInfo method)
        {
            return !Attribute.IsDefined(method, typeof(DoNotLogAttribute));
        }

        internal class ErrorResponseModel
        {
            public ErrorResponseModel(int statusCode, string executionError)
            {
                StatusCode = statusCode;
                ExecutionError = executionError;
            }

            public int StatusCode { get; }
            public string ExecutionError { get; }
        }

        protected void EnsureUserRight(UserRight right) 
        {
            WebSession.EnsureUserRight(right);
        }
    }
}
