using System;
using System.Collections;
using System.Collections.Generic;

namespace Robowire.RobOrm.Core.EntityModel
{
    public class EntityHolder : IEntitySet
    {
        public IEntity Value { get; private set; }

        public IEnumerator<IEntity> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEntity Find(object primaryKeyValue)
        {
            if ((Value == null) || !Value.PrimaryKeyValue.Equals(primaryKeyValue))
            {
                return null;
            }

            return Value;
        }

        public void Add(IEntity entity)
        {
            Value = entity;
        }
    }
}
