using System;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;

namespace Robowire.RoboApi.Internal
{
    internal sealed class RaControllerFactory : IControllerFactory
    {
        private readonly IContainer m_container;
        private readonly Action<RequestContext, IServiceLocator> m_requestScopeInitializer;

        public RaControllerFactory(IContainer container, Action<RequestContext, IServiceLocator> requestScopeInitializer)
        {
            m_container = container;
            m_requestScopeInitializer = requestScopeInitializer;
        }

        public IController CreateController(RequestContext requestContext, string controllerName)
        {
            var locator = m_container.GetLocator();

            m_requestScopeInitializer?.Invoke(requestContext, locator);

            var nameExtractor = locator.Get<IControllerNameExtractor>();

            var ctlrName = nameExtractor.GetControllerName(requestContext, controllerName);

            var index = locator.Get<ControllerIndex>();
            var controllerType = index.GetControllerType(ctlrName);

            var controller = locator.Get(controllerType) as IController;

            if (controller == null)
            {
                throw new InvalidOperationException($"Invalid controller name \"{ctlrName}\"");
            }

            return controller;
        }

        public SessionStateBehavior GetControllerSessionBehavior(RequestContext requestContext, string controllerName)
        {
            return SessionStateBehavior.Default;
        }

        public void ReleaseController(IController controller)
        {
            var dcontroller = controller as ILocatorBoundController;
            dcontroller?.Locator?.Dispose();
        }
    }
}
