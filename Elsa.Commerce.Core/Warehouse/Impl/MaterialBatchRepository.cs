using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Impl
{
    public class MaterialBatchRepository : IMaterialBatchRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        public MaterialBatchRepository(IDatabase database, ISession session)
        {
            m_database = database;
            m_session = session;
        }

        public MaterialBatchComponent GetBatchById(int id)
        {
            var entity = GetBatchQuery().Where(b => b.Id == id).Execute().FirstOrDefault();
            if (entity == null)
            {
                return null;
            }

            return MapToModel(entity);
        }

        private MaterialBatchComponent MapToModel(IMaterialBatch entity)
        {
            var model = new MaterialBatchComponent(entity);

            foreach (var component in entity.Components)
            {
                var componentModel = GetBatchById(component.ComponentId);
                componentModel.ComponentAmount = component.Volume;
                componentModel.ComponentUnit = component.Unit;
                model.Components.Add(componentModel);
            }

            return model;
        }

        public IEnumerable<MaterialBatchComponent> GetMaterialBatches(DateTime from, DateTime to, bool excludeCompositions, int? materialId)
        {
            var query = GetBatchQuery().Where(b => b.Created >= from && b.Created <= to);
            if (materialId != null)
            {
                query = query.Where(b => b.MaterialId == materialId.Value);
            }

            var entities =
                query.Execute().Where(b => (!excludeCompositions) || (!b.Components.Any())).Select(MapToModel);

            return entities;
        }

        private IQueryBuilder<IMaterialBatch> GetBatchQuery()
        {
            return
                m_database.SelectFrom<IMaterialBatch>()
                    .Join(b => b.Author)
                    .Join(b => b.Material)
                    .Join(b => b.Unit)
                    .Join(b => b.Components)
                    .Join(b => b.Components.Each().Unit)
                    .Where(b => b.ProjectId == m_session.Project.Id);
        }
    }
}
