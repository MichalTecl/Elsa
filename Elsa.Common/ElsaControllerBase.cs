using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;

using Robowire.RoboApi.Extensibility;

namespace Elsa.Common
{
    public abstract class ElsaControllerBase : IControllerInterceptor
    {
        protected ElsaControllerBase(IWebSession webSession)
        {
            WebSession = webSession;
        }

        protected IWebSession WebSession { get; }
        
        public bool OnRequest(object controller, RequestContext request)
        {
            //WebSession.Initialize(request);

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
            
            defaultResultWrite(defaultInvocation());
        }

        public void OnException(object controller, MethodInfo method, RequestContext context, object[] parameters, Exception exception)
        {
            context.HttpContext.Response.StatusDescription = exception.Message;
            
            if (exception is UnauthorizedAccessException)
            {
                context.HttpContext.Response.StatusCode = 401;
            }
            else
            {
                context.HttpContext.Response.StatusCode = 500;
            }

        }
    }
}
