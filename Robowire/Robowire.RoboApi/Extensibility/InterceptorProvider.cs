using System.Web.Mvc;

using Robowire.RoboApi.Convention.Default;

namespace Robowire.RoboApi.Extensibility
{
    public static class InterceptorProvider
    {
        private static readonly DefaultInterceptor s_defaultInterceptor = new DefaultInterceptor();

        public static IControllerInterceptor GetInterceptor(IController controller)
        {
            return (controller as IControllerInterceptor) 
                ?? (controller as IHaveInterceptor)?.Interceptor
                ?? s_defaultInterceptor;
        }
    }
}
