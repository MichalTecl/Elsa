using System;
using System.Linq;

using Robowire.RoboApi.Convention.Default;
using Robowire.RoboApi.Internal;

namespace Robowire.RoboApi
{
    public class ControllerAttribute : Attribute, ISelfSetupAttribute
    {
        private readonly string m_controllerName;
        private readonly Type m_proxyBuilderType;
        private readonly Type m_callBuilderType;
        
        public ControllerAttribute(string controllerName, Type proxyBuilderType = null, Type callBuilderType = null)
        {
            m_controllerName = controllerName;
            m_proxyBuilderType = proxyBuilderType ?? typeof(ControllerProxyBuilder);
            m_callBuilderType = callBuilderType ?? typeof(DefaultCallBuilder);
        }

        public string Name => m_controllerName;

        public void Setup(Type markedType, IContainerSetup setup)
        {
            if (!setup.GetRegisteredPlugins<ControllerCollectorPlugin>().Any())
            {
                setup.RegisterPlugin(s => s.CustomInstanceCreators.Add(new ControllerCollectorPlugin()));
            }

            setup.For(markedType)
                .Use(markedType)
                .WithBehavior<ControllerBehavior>(SetupBehavior);
        }

        protected virtual void SetupBehavior(ControllerBehavior behavior)
        {
            behavior.ControllerName = m_controllerName;
            behavior.ProxyBuilderType = m_proxyBuilderType;
            behavior.CallBuilderType = m_callBuilderType;
        }
    }
}
