﻿using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Commerce;
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
        private readonly IVirtualProductRepository m_virtualProductRepository;
        private readonly ICache m_cache;
        
        public MaterialBatchRepository(IDatabase database, ISession session, IUnitConversionHelper conversionHelper, IMaterialRepository materialRepository, IVirtualProductRepository virtualProductRepository, ICache cache)
        {
            m_database = database;
            m_session = session;
            m_conversionHelper = conversionHelper;
            m_materialRepository = materialRepository;
            m_virtualProductRepository = virtualProductRepository;
            m_cache = cache;
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

        public IEnumerable<MaterialBatchComponent> GetMaterialBatches(
            DateTime from,
            DateTime to,
            bool excludeCompositions,
            int? materialId,
            bool includeLocked = false,
            bool includeClosed = false)
        {
            var query = GetBatchQuery().Where(b => b.Created >= from && b.Created <= to);
            
            if (materialId != null)
            {
                query = query.Where(b => b.MaterialId == materialId.Value);
            }

            if (!includeLocked)
            {
                query = query.Where(b => b.LockDt == null);
            }

            if (!includeClosed)
            {
                query = query.Where(b => b.CloseDt == null);
            }

            var entities = query.Execute()
                .Where(b => (!excludeCompositions) || (!b.Components.Any()))
                .Select(MapToModel);

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

        public IEnumerable<IMaterialStockEvent> GetBatchEvents(int materialBatchId)
        {
            var key = $"stockEvents_for_batch_{materialBatchId}";

            return m_cache.ReadThrough(
                key,
                TimeSpan.FromHours(1),
                () =>
                    m_database.SelectFrom<IMaterialStockEvent>()
                        .Where(m => m.ProjectId == m_session.Project.Id)
                        .Where(m => m.BatchId == materialBatchId)
                        .Execute());
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