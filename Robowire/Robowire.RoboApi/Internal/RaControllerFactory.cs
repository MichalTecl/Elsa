using System;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;

namespace Robowire.RoboApi.Internal
{
    internal sealed class RaControllerFactory : IControllerFactory
    {
        private readonly IContainer _container;
        private readonly Action<RequestContext, IServiceLocator> _requestScopeInitializer;
        private readonly Lazy<IControllerNameExtractor> _nameExtractor;
        private readonly Lazy<ControllerIndex> _controllerIndex;

        public RaControllerFactory(IContainer container, Action<RequestContext, IServiceLocator> requestScopeInitializer)
        {
            _container = container;
            _requestScopeInitializer = requestScopeInitializer;

            // Resolved lazily (on the first request), so the factory can be installed before the rest
            // of the container setup is done and the container gets compiled only once at startup.
            _nameExtractor = new Lazy<IControllerNameExtractor>(Resolve<IControllerNameExtractor>, LazyThreadSafetyMode.ExecutionAndPublication);
            _controllerIndex = new Lazy<ControllerIndex>(Resolve<ControllerIndex>, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private T Resolve<T>()
        {
            using (var locator = _container.GetLocator())
            {
                return locator.Get<T>();
            }
        }

        public IController CreateController(RequestContext requestContext, string controllerName)
        {
            var locator = _container.GetLocator();

            var ctlrName = _nameExtractor.Value.GetControllerName(requestContext, controllerName);
            var controllerType = _controllerIndex.Value.GetControllerType(ctlrName);
            var controllerAttribute = controllerType.GetCustomAttributes(typeof(ControllerAttribute), true)
                .OfType<ControllerAttribute>()
                .FirstOrDefault();

            if (controllerAttribute?.SuppressSessionCookieWrite == true)
            {
                RequestContextFlags.SetSuppressSessionCookieWrite(requestContext.HttpContext.Items, true);
            }

            _requestScopeInitializer?.Invoke(requestContext, locator);

            var controller = locator.Get(controllerType) as IController;

            if (controller == null)
            {
                throw new InvalidOperationException($"Invalid controller name \"{ctlrName}\"");
            }

            return controller;
        }

        public SessionStateBehavior GetControllerSessionBehavior(RequestContext requestContext, string controllerName)
        {
            var ctlrName = _nameExtractor.Value.GetControllerName(requestContext, controllerName);
            var controllerType = _controllerIndex.Value.GetControllerType(ctlrName);
            var controllerAttribute = controllerType.GetCustomAttributes(typeof(ControllerAttribute), true)
                .OfType<ControllerAttribute>()
                .FirstOrDefault();

            if (controllerAttribute?.SuppressSessionCookieWrite == true)
            {
                return SessionStateBehavior.Disabled;
            }

            return SessionStateBehavior.Default;
        }

        public void ReleaseController(IController controller)
        {
            var dcontroller = controller as ILocatorBoundController;
            dcontroller?.Locator?.Dispose();
        }
    }
}
