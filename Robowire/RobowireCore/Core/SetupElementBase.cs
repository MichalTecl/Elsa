using System;
using System.Collections.Generic;

using Robowire.Plugin;

namespace Robowire.Core
{
    public abstract class SetupElementBase : IWithBehavior
    {
        private readonly InstanceRecord m_record;

        private readonly IEnumerable<IPlugin> m_plugins;

        protected SetupElementBase(InstanceRecord record, IEnumerable<IPlugin> plugins)
        {
            m_record = record;
            m_plugins = plugins;
        }

        public IWithBehavior WithBehavior<TSetupObject>(Action<TSetupObject> setup) where TSetupObject : IBehavior, new()
        {
            var behavior = new TSetupObject();
            behavior.BindTo(m_record);

            setup(behavior);

            m_record.AddBehavior(behavior);

            return this;
        }
    }
}
