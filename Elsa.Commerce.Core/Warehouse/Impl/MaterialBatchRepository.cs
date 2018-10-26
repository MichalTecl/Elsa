using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Impl
{
    public class MaterialBatchRepository : IMaterialBatchRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly IUnitConversionHelper m_conversionHelper;
        private readonly IMaterialRepository m_materialRepository;

        public MaterialBatchRepository(IDatabase database, ISession session, IUnitConversionHelper conversionHelper, IMaterialRepository materialRepository)
        {
            m_database = database;
            m_session = session;
            m_conversionHelper = conversionHelper;
            m_materialRepository = materialRepository;
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

        public MaterialBatchComponent SaveMaterialBatch(
            int id,
            IMaterial material,
            decimal amount,
            IMaterialUnit unit,
            string batchNr,
            DateTime receiveDt,
            decimal price)
        {
            if (material.ProjectId != m_session.Project.Id || unit.ProjectId != m_session.Project.Id)
            {
                throw new InvalidOperationException("Illegal entity reference");
            }

            var m = m_materialRepository.GetMaterialById(material.Id);
            if (m.Components.Any())
            {
                throw new InvalidOperationException($"Materiál '{material.Name}' nelze naskladnit, protože se skládá z jiných materiálů. Použijte prosím funkci Výroba");
            }

            MaterialBatchComponent result;
            using (var tx = m_database.OpenTransaction())
            {
                IMaterialBatch entity = null;
                if (id > 0)
                {
                    entity =
                        GetBatchQuery()
                            .Where(b => b.Id == id && b.ProjectId == m_session.Project.Id)
                            .Execute()
                            .FirstOrDefault();
                    if (entity == null)
                    {
                        throw new InvalidOperationException("Illegal entity reference");
                    }
                }
                else
                {
                    entity = m_database.New<IMaterialBatch>();
                    entity.AuthorId = m_session.User.Id;
                    entity.ProjectId = m_session.Project.Id;
                }

                if (!m_conversionHelper.AreCompatible(material.NominalUnitId, unit.Id))
                {
                    throw new InvalidOperationException(
                              $"Nelze použít jednotku '{unit.Symbol}' pro materiál '{material.Name}' protože není převoditelná na nominální jednotku materiálu '{material.NominalUnit}'");
                }

                entity.BatchNumber = batchNr;
                entity.Created = receiveDt;
                entity.MaterialId = material.Id;
                entity.Volume = amount;
                entity.UnitId = unit.Id;
                entity.Price = price;
                entity.Note = string.Empty;
                m_database.Save(entity);

                result = GetBatchById(entity.Id);

                if (result.Components.Any())
                {
                    throw new InvalidOperationException($"Materiál '{material.Name}' nelze naskladnit, protože se skládá z jiných materiálů. Použijte prosím funkci Výroba");
                }

                tx.Commit();
            }

            return result;
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
