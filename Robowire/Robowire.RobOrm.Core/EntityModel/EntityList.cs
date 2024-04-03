using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Robowire.RobOrm.Core.EntityModel
{
    public class EntityList : IEntitySet
    {
        private readonly List<IEntity> m_items = new List<IEntity>();

        public IEnumerator<IEntity> GetEnumerator()
        {
            return m_items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEntity Find(object primaryKeyValue)
        {
            return m_items.FirstOrDefault(i => primaryKeyValue.Equals(i.PrimaryKeyValue));
        }

        public void Add(IEntity entity)
        {
            m_items.Add(entity);
        }
    }
}
