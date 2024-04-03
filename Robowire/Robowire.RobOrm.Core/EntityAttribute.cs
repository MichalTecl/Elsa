using System;
using System.Linq;

using Robowire.Behavior;
using Robowire.RobOrm.Core.DefaultRules;
using Robowire.RobOrm.Core.EntityGeneration;
using Robowire.RobOrm.Core.Internal;

namespace Robowire.RobOrm.Core
{
    public class EntityAttribute : Attribute, ISelfSetupAttribute
    {
        public string EntityName { get; set; }

        public string PrimaryKeyProperty { get; set; }
        
        public void Setup(Type markedType, IContainerSetup setup)
        {
            if (!setup.GetRegisteredPlugins<EntityPlugin>().Any())
            {
                setup.RegisterPlugin(p => p.CustomInstanceCreators.Add(new EntityPlugin(new DefaultRobOrmSetup())));
            }

            if (!setup.GetRegisteredPlugins<EntityCollectorPlugin>().Any())
            {
                setup.RegisterPlugin(p => p.CustomInstanceCreators.Add(new EntityCollectorPlugin()));
                setup.For<IEntityCollector>();
            }

            var entityName = EntityName;

            if (string.IsNullOrWhiteSpace(entityName))
            {
                entityName = NamingHelper.GetDefaultEntityName(markedType);
            }

            var pkProperty = PrimaryKeyProperty;
            if (string.IsNullOrWhiteSpace(pkProperty))
            {
                pkProperty = "Id";
            }

            if (ReflectionUtil.GetProperty(markedType, pkProperty) == null)
            {
                throw new InvalidOperationException($"{markedType.Name} - Primary Key property \"{pkProperty}\" not found");
            }

            setup.For(markedType)
                .WithBehavior<EntityBehavior>(db => 
                {
                    db.EntityName = entityName;
                    db.PrimaryKeyProperty = pkProperty;
                })
                .WithBehavior<LifecycleBehavior>(lb => lb.AlwaysNewInstance = true)
                .WithBehavior<DisposeBehavior>(db => db.Dispose = false);
        }
    }
}
