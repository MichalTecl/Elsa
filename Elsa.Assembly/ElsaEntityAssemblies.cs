using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using RobOrm.Core;

using Robowire.Diagnostics;

namespace Elsa.Assembly
{
    /// <summary>
    /// Finds Elsa assemblies which can contain RobOrm entities: all Elsa.* assemblies referenced (transitively)
    /// by Elsa.Assembly which reference RobOrm.Core. A new project with entities is found automatically
    /// once it is referenced from Elsa.Assembly (directly or through another Elsa project).
    /// </summary>
    public static class ElsaEntityAssemblies
    {
        private const string ELSA_ASSEMBLY_PREFIX = "Elsa.";

        public static IReadOnlyList<System.Reflection.Assembly> Find()
        {
            var assemblies = AssemblyDiscovery.FindEntityAssemblies(
                new[] { typeof(ElsaEntityAssemblies).Assembly },
                name => name.Name.StartsWith(ELSA_ASSEMBLY_PREFIX, StringComparison.OrdinalIgnoreCase),
                StartupLog.Write);

            StartupLog.Write($"ElsaEntityAssemblies: {assemblies.Count} assemblies referencing RobOrm (entity candidates): {string.Join(", ", assemblies.Select(a => a.GetName().Name))}");

            return assemblies;
        }
    }
}
