using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RobOrm.Core
{
    /// <summary>
    /// Helps to find assemblies containing entities without listing them explicitly.
    /// </summary>
    public static class AssemblyDiscovery
    {
        /// <summary>
        /// Walks the references of the root assemblies (transitively) and returns all assemblies
        /// referencing RobOrm.Core - i.e. the assemblies which can contain entities. The root assemblies are included
        /// when they reference RobOrm.Core.
        /// </summary>
        /// <param name="roots">Where to start the walk.</param>
        /// <param name="filter">
        /// Optional filter of assembly names - assemblies not passing the filter are neither returned nor walked through
        /// (use it to skip framework and third party assemblies). The root assemblies are always walked through.
        /// </param>
        /// <param name="log">Optional log of assemblies which could not be loaded.</param>
        public static IReadOnlyList<Assembly> FindEntityAssemblies(
            IEnumerable<Assembly> roots,
            Func<AssemblyName, bool> filter = null,
            Action<string> log = null)
        {
            var robOrmName = typeof(EntityAttribute).Assembly.GetName().Name;

            var result = new List<Assembly>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<Assembly>();

            foreach (var root in roots)
            {
                if (visited.Add(root.GetName().Name))
                {
                    queue.Enqueue(root);
                }
            }

            while (queue.Count > 0)
            {
                var assembly = queue.Dequeue();
                var references = assembly.GetReferencedAssemblies();

                if (references.Any(r => string.Equals(r.Name, robOrmName, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(assembly);
                }

                foreach (var reference in references)
                {
                    if ((filter != null) && !filter(reference))
                    {
                        continue;
                    }

                    if (!visited.Add(reference.Name))
                    {
                        continue;
                    }

                    try
                    {
                        queue.Enqueue(Assembly.Load(reference));
                    }
                    catch (Exception ex)
                    {
                        log?.Invoke($"RobOrm: cannot load assembly {reference.FullName} referenced by {assembly.GetName().Name}: {ex.Message}");
                    }
                }
            }

            return result;
        }
    }
}
