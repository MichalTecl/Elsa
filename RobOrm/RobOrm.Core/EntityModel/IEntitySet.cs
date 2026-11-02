using System.Collections.Generic;

namespace RobOrm.Core.EntityModel
{
    public interface IEntitySet : IEnumerable<IEntity>
    {
        IEntity Find(object primaryKeyValue);

        void Add(IEntity entity);
    }
}
