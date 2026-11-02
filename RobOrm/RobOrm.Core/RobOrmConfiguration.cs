using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using RobOrm.Core.EntityGeneration;

namespace RobOrm.Core
{
    /// <summary>
    /// Entry point of RobOrm setup:
    /// <code>
    /// var entities = RobOrmConfiguration.Create()
    ///     .ScanAssemblies(assemblies)
    ///     .BuildEntityRegistry();
    /// </code>
    /// Database specific packages add their own extensions (e.g. UseSqlServer).
    /// </summary>
    public sealed class RobOrmConfiguration
    {
        private readonly List<Type> _entityTypes = new List<Type>();
        private readonly HashSet<Type> _knownTypes = new HashSet<Type>();
        private readonly List<string> _scannedAssemblies = new List<string>();
        private Action<string> _log;

        private RobOrmConfiguration()
        {
        }

        public static RobOrmConfiguration Create()
        {
            return new RobOrmConfiguration();
        }

        /// <summary>
        /// Diagnostic messages written during the setup and the schema migration.
        /// </summary>
        public Action<string> Log => _log ?? (message => { });

        public RobOrmConfiguration WithLog(Action<string> log)
        {
            _log = log;
            return this;
        }

        /// <summary>
        /// Registers all interfaces marked with <see cref="EntityAttribute"/> found in the assembly.
        /// </summary>
        public RobOrmConfiguration ScanAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (_scannedAssemblies.Contains(assembly.FullName))
            {
                return this;
            }

            _scannedAssemblies.Add(assembly.FullName);

            foreach (var type in assembly.GetTypes())
            {
                if (Attribute.IsDefined(type, typeof(EntityAttribute), false))
                {
                    AddEntity(type);
                }
            }

            return this;
        }

        public RobOrmConfiguration ScanAssemblies(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                ScanAssembly(assembly);
            }

            return this;
        }

        public RobOrmConfiguration AddEntity(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            if (!entityType.IsInterface)
            {
                throw new InvalidOperationException($"{entityType.FullName} is marked with {nameof(EntityAttribute)}, but only interfaces can be entities");
            }

            if (_knownTypes.Add(entityType))
            {
                _entityTypes.Add(entityType);
            }

            return this;
        }

        public RobOrmConfiguration AddEntity<T>() where T : class
        {
            return AddEntity(typeof(T));
        }

        /// <summary>
        /// Generates and validates implementations of all registered entities.
        /// Invalid entity definitions throw here, so the application fails on start.
        /// </summary>
        public EntityRegistry BuildEntityRegistry()
        {
            Log($"RobOrm: generating {_entityTypes.Count} entities from {_scannedAssemblies.Count} assemblies");

            var registry = new EntityRegistry(EntityTypeCache.GetOrCreateAll(_entityTypes));

            Log($"RobOrm: {registry.Count} entities ready");

            return registry;
        }

        /// <summary>
        /// Names of the scanned assemblies (diagnostics).
        /// </summary>
        public IEnumerable<string> ScannedAssemblies => _scannedAssemblies.ToList();
    }
}
