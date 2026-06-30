using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;

namespace Robowire.RoboApi.Internal
{
    internal sealed class RaControllerFactory : IControllerFactory
    {
        private readonly IContainer m_container;
        private readonly Action<RequestContext, IServiceLocator> m_requestScopeInitializer;
        private readonly IControllerNameExtractor m_nameExtractor;
        private readonly ControllerIndex m_controllerIndex;

        public RaControllerFactory(IContainer container, Action<RequestContext, IServiceLocator> requestScopeInitializer)
        {
            m_container = container;
            m_requestScopeInitializer = requestScopeInitializer;

            using (var locator = m_container.GetLocator())
            {
                m_nameExtractor = locator.Get<IControllerNameExtractor>();
                m_controllerIndex = locator.Get<ControllerIndex>();
            }
        }

        public IController CreateController(RequestContext requestContext, string controllerName)
        {
            var locator = m_container.GetLocator();

            var ctlrName = m_nameExtractor.GetControllerName(requestContext, controllerName);
            var controllerType = m_controllerIndex.GetControllerType(ctlrName);
            var controllerAttribute = controllerType.GetCustomAttributes(typeof(ControllerAttribute), true)
                .OfType<ControllerAttribute>()
                .FirstOrDefault();

            if (controllerAttribute?.SuppressSessionCookieWrite == true)
            {
                RequestContextFlags.SetSuppressSessionCookieWrite(requestContext.HttpContext.Items, true);
            }

            m_requestScopeInitializer?.Invoke(requestContext, locator);

            var controller = locator.Get(controllerType) as IController;

            if (controller == null)
            {
                throw new InvalidOperationException($"Invalid controller name \"{ctlrName}\"");
            }

            return controller;
        }

        public SessionStateBehavior GetControllerSessionBehavior(RequestContext requestContext, string controllerName)
        {
            var ctlrName = m_nameExtractor.GetControllerName(requestContext, controllerName);
            var controllerType = m_controllerIndex.GetControllerType(ctlrName);
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
