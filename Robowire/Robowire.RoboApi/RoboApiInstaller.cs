using System;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;

using Robowire.RoboApi.Internal;

namespace Robowire.RoboApi
{
    public class RoboApiInstaller
    {
        public void Install(
            ControllerBuilder controllerBuilder,
            IContainer container,
            Action<RequestContext, IServiceLocator> requestScopeInitializer,
            params Assembly[] controllerAssemblies)
        {
            container.Setup(
                setup =>
                    {
                        setup.RegisterPlugin(
                            ps => ps.CustomInstanceCreators.Add(new ControllerIndex.ControllerIndexPlugin()));

                        RegisterControllerNameExtractor(setup);

                        foreach (var asm in controllerAssemblies)
                        {
                            setup.ScanAssembly(asm);
                        }

                        setup.For<ControllerIndex>().Use<ControllerIndex>();
                    });

            var factory = new RaControllerFactory(container, requestScopeInitializer);

            controllerBuilder?.SetControllerFactory(factory);
        }

        protected virtual void RegisterControllerNameExtractor(IContainerSetup setup)
        {
            setup.For<IControllerNameExtractor>().Use<DefaultControllerNameExtractor>();
        }
    }
}
