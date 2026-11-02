using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RobOrm.Core.EntityModel
{
    public class EntityList : IEntitySet
    {
        private readonly List<IEntity> _items = new List<IEntity>();

        public IEnumerator<IEntity> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEntity Find(object primaryKeyValue)
        {
            return _items.FirstOrDefault(i => primaryKeyValue.Equals(i.PrimaryKeyValue));
        }

        public void Add(IEntity entity)
        {
            _items.Add(entity);
        }
    }
}
