using System;
using System.Collections.Generic;
using System.Linq;

using RobOrm.Core.EntityModel;

namespace RobOrm.Core.Query.Reader
{
    public class ResultSetReader
    {
        public static IEnumerable<T> Read<T>(IDataReader reader, IEntityFactory factory) where T : class
        {
            var result = new EntityList();
            
            while (reader.Read())
            {
                ReadEntity(typeof(T), result, reader.GetDeeperReader(typeof(T).Name), factory);
            }

            return result.OfType<T>();
        }

        private static void ReadEntity(Type entityType, IEntitySet target, IDataRecord reader, IEntityFactory factory)
        {
            var prototype = factory.TryCreate(entityType) as IEntity;
            if (prototype == null)
            {
                throw new InvalidOperationException($"There is no IEntity implementing object setup for {entityType.Name}");
            }

            if (!prototype.ReadPrimaryKey(reader))
            {
                return;
            }

            var entity = target.Find(prototype.PrimaryKeyValue);
            if (entity == null)
            {
                prototype.ReadSelf(reader);
                target.Add(prototype);
                entity = prototype;
            }

            foreach (var referrenceProperty in entity.GetReferenceProperties())
            {
                var list = entity.OpenProperty(referrenceProperty.Item1);
                var propReader = reader.GetDeeperReader(referrenceProperty.Item1);

                ReadEntity(referrenceProperty.Item2, list, propReader, factory);
            }
        }
    }
}
