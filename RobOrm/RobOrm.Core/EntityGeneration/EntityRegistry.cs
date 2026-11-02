using System;
using System.Collections.Generic;
using System.Linq;

using RobOrm.Core.DefaultRules;

namespace RobOrm.Core.EntityGeneration
{
    /// <summary>
    /// The set of entities known to RobOrm. Creates entity instances and provides the entity list for the schema migration.
    /// Create it with <see cref="RobOrmConfiguration"/>.
    /// </summary>
    public sealed class EntityRegistry : IEntityCollector, IEntityFactory
    {
        private readonly List<Type> _entityTypes;
        private readonly Dictionary<Type, EntityTypeInfo> _entities;

        internal EntityRegistry(IEnumerable<EntityTypeInfo> entities)
        {
            _entities = new Dictionary<Type, EntityTypeInfo>();
            _entityTypes = new List<Type>();

            foreach (var entity in entities)
            {
                if (_entities.ContainsKey(entity.EntityType))
                {
                    continue;
                }

                _entities.Add(entity.EntityType, entity);
                _entityTypes.Add(entity.EntityType);
            }
        }

        public int Count => _entityTypes.Count;

        public IEnumerable<Type> GetEntityTypes()
        {
            return _entityTypes.AsReadOnly();
        }

        public bool IsEntity(Type type)
        {
            return (type != null) && _entities.ContainsKey(type);
        }

        public EntityTypeInfo GetEntityInfo(Type entityType)
        {
            EntityTypeInfo info;
            if (!_entities.TryGetValue(entityType, out info))
            {
                throw new InvalidOperationException($"{entityType} is not a registered RobOrm entity");
            }

            return info;
        }

        public T Create<T>() where T : class
        {
            return (T)Create(typeof(T));
        }

        public object Create(Type entityType)
        {
            var entity = TryCreate(entityType);
            if (entity == null)
            {
                throw new InvalidOperationException($"There is no IEntity implementing object setup for {entityType.Name}");
            }

            return entity;
        }

        public object TryCreate(Type entityType)
        {
            EntityTypeInfo info;
            if ((entityType == null) || !_entities.TryGetValue(entityType, out info))
            {
                return null;
            }

            return info.CreateInstance();
        }
    }

    /// <summary>
    /// Process wide cache of the generated entity implementations - every entity interface is generated only once.
    /// </summary>
    internal static class EntityTypeCache
    {
        private static readonly object s_lock = new object();
        private static readonly Dictionary<Type, EntityTypeInfo> s_cache = new Dictionary<Type, EntityTypeInfo>();
        private static readonly IEntityNamingConvention s_convention = new DefaultEntityNamingConvention();

        public static EntityTypeInfo GetOrCreate(Type entityType)
        {
            lock (s_lock)
            {
                EntityTypeInfo info;
                if (s_cache.TryGetValue(entityType, out info))
                {
                    return info;
                }

                info = EntityTypeInfo.Build(entityType, s_convention);
                info.ImplementationType = EntityTypeEmitter.Emit(info);

                s_cache.Add(entityType, info);

                return info;
            }
        }

        public static IEnumerable<EntityTypeInfo> GetOrCreateAll(IEnumerable<Type> entityTypes)
        {
            return entityTypes.Select(t => GetOrCreate(t)).ToList();
        }
    }
}
