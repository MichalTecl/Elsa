using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

using Robowire.Plugin;
using Robowire.Plugin.Flow;

namespace Robowire.Core.LocatorGeneration
{
    /// <summary>
    /// Describes everything from the container setup the generated locator code depends on.
    /// Together with <see cref="ApplicationBuild.Hash"/> it identifies the compiled locator assembly,
    /// so it can be loaded from the cache without generating the code.
    /// </summary>
    internal static class SetupFingerprint
    {
        private const string FORMAT_VERSION = "2";

        public static string Compute(IEnumerable<InstanceRecord> records, IPluginCollection plugins)
        {
            var sb = new StringBuilder();

            sb.AppendLine(FORMAT_VERSION);

            AppendPlugins(sb, "custom", plugins.CustomInstanceCreators);
            AppendPlugins(sb, "default", plugins.DefaultInstanceCreators);
            AppendPlugins(sb, "after", plugins.AfterNewInstanceCreated);
            AppendPlugins(sb, "lifecycle", plugins.LifecyclePlugins);

            foreach (var record in records)
            {
                sb.Append("R|").Append(TypeKey(record.InterfaceType))
                    .Append('|').Append(TypeKey(record.ImplementingType))
                    .Append('|').AppendLine(record.Factory?.Name ?? "-");

                if (record.ConstructorParameters != null)
                {
                    foreach (var param in record.ConstructorParameters)
                    {
                        sb.Append(" P|").Append(param.ParameterName ?? "-")
                            .Append('|').Append(TypeKey(param.ParameterType))
                            .Append('|').AppendLine(param.ValueProvider?.Name ?? "-");
                    }
                }

                foreach (var behavior in record.Behaviors)
                {
                    sb.Append(" B|").Append(TypeKey(behavior.GetType()));

                    foreach (var property in behavior.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && (p.GetIndexParameters().Length == 0)).OrderBy(p => p.Name, StringComparer.Ordinal))
                    {
                        sb.Append('|').Append(property.Name).Append('=').Append(ValueKey(property.GetValue(behavior)));
                    }

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static void AppendPlugins(StringBuilder sb, string group, IEnumerable<IPlugin> plugins)
        {
            sb.Append(group);

            foreach (var plugin in plugins)
            {
                sb.Append('|').Append(TypeKey(plugin.GetType()));
            }

            sb.AppendLine();
        }

        private static string TypeKey(Type type)
        {
            return type?.AssemblyQualifiedName ?? "-";
        }

        private static string ValueKey(object value)
        {
            if (value == null)
            {
                return "-";
            }

            var type = value as Type;
            if (type != null)
            {
                return TypeKey(type);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
