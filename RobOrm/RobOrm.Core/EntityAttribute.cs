using System;

namespace RobOrm.Core
{
    /// <summary>
    /// Marks an interface as a RobOrm entity. The implementation of the interface is generated at runtime
    /// (see <see cref="RobOrmConfiguration"/>).
    /// </summary>
    public class EntityAttribute : Attribute
    {
        public string EntityName { get; set; }

        public string PrimaryKeyProperty { get; set; }
    }
}
