using System;

namespace RobOrm.Core
{
    /// <summary>
    /// Creates new instances of entities.
    /// </summary>
    public interface IEntityFactory
    {
        T Create<T>() where T : class;

        object Create(Type entityType);

        /// <summary>
        /// Returns null when the type is not a registered entity.
        /// </summary>
        object TryCreate(Type entityType);
    }
}
