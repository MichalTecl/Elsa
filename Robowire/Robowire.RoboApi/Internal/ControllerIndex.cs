using Robowire.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using CodeGeneration;
using CodeGeneration.Primitives;
using Robowire.Core;

namespace Robowire.RoboApi.Internal
{
    public sealed class ControllerIndex
    {
        private readonly Dictionary<string, Type> m_index;

        public ControllerIndex(IEnumerable<KeyValuePair<string, Type>> source)
        {
            m_index = source.ToDictionary(kv => kv.Key.ToLowerInvariant(), kv => kv.Value);
        }

        public Type GetControllerType(string controllerName)
        {
            var key = controllerName.Trim().ToLowerInvariant();

            Type t;
            if (!m_index.TryGetValue(key, out t))
            {
                throw new InvalidOperationException($"Unknown controller \"{controllerName}\"");
            }

            return t;
        }

        public sealed class ControllerIndexPlugin : IPlugin
        {
            public INamedReference GenerateFactoryMethod(IServiceSetupRecord setup, Dictionary<string, INamedReference> ctorParamValueFactoryFields, INamedReference valueFactoryField, IClassBuilder locatorBuilder, INamedReference previousPluginMethod)
            {
                var fm = locatorBuilder.HasMethod($"ControllerIndexFactory_{Guid.NewGuid():N}").Returns<ControllerIndex>().WithModifier("private");

                fm.Body.Write("return new ").Write(typeof(ControllerIndex)).Write("(this.GetRoboapiControllerIndexFactory())").EndStatement();

                return fm;
            }

            public IPlugin InheritToChildContainer()
            {
                return this;
            }

            public bool IsApplicable(IServiceSetupRecord setup)
            {
                return setup.InterfaceType == typeof(ControllerIndex);
            }
        }
    }
}
