using System;
using System.Collections.Generic;
using System.Reflection;

using Robowire.Plugin;

namespace Robowire.Core
{
    public class InstanceRecord : IServiceSetupRecord
    {
        private readonly List<IBehavior> m_behaviors = new List<IBehavior>();

        public Type InterfaceType { get; set; }

        public Type ImplementingType { get; set; }

        public NamedFactory Factory { get; set; }

        public bool HasValueFactory => Factory != null;
        
        public List<CtorParamSetupRecord> ConstructorParameters { get; set; }

        public List<IPlugin> ApplicablePlugins { get; set; }

        public ConstructorInfo PreferredConstructor { get; set; }

        public IEnumerable<IBehavior> Behaviors => m_behaviors;

        IEnumerable<CtorParamSetupRecord> IServiceSetupRecord.ConstructorParameters => ConstructorParameters;

        IEnumerable<IPlugin> IServiceSetupRecord.ApplicablePlugins => ApplicablePlugins;

        public void AddBehavior(IBehavior behavior)
        {
            m_behaviors.Add(behavior);
        }
    }
}
