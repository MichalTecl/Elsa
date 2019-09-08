﻿using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.CurrencyRates;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse.Impl.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;
using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Impl
{
    public class MaterialBatchRepository2 : IMaterialBatchRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly IUnitConversionHelper m_conversionHelper;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IVirtualProductRepository m_virtualProductRepository;
        private readonly ICache m_cache;
        private readonly Lazy<IMaterialBatchFacade> m_materialBatchFacade;
        private readonly ISupplierRepository m_supplierRepository;
        private readonly ICurrencyConversionHelper m_currencyConversionHelper;
        private readonly IServiceLocator m_serviceLocator;

        public MaterialBatchRepository2(IDatabase database,
            ISession session,
            IUnitConversionHelper conversionHelper,
            IMaterialRepository materialRepository,
            IVirtualProductRepository virtualProductRepository,
            ICache cache,
            Lazy<IMaterialBatchFacade> materialBatchFacade,
            ISupplierRepository supplierRepository, 
            ICurrencyConversionHelper currencyConversionHelper, IServiceLocator serviceLocator)
        {
            m_database = database;
            m_session = session;
            m_conversionHelper = conversionHelper;
            m_materialRepository = materialRepository;
            m_virtualProductRepository = virtualProductRepository;
            m_cache = cache;
            m_materialBatchFacade = materialBatchFacade;
            m_supplierRepository = supplierRepository;
            m_currencyConversionHelper = currencyConversionHelper;
            m_serviceLocator = serviceLocator;
        }

        public MaterialBatchComponent GetBatchById(int id)
        {
            var entity = GetBatchQuery().Where(b => b.Id == id).Execute().FirstOrDefault();
            return entity == null ? null : MapToModel(entity);
        }

        public MaterialBatchComponent GetBatchByNumber(int materialId, string batchNumber)
        {
            var batch =
                GetBatchQuery()
                    .Where(b => (b.MaterialId == materialId) && (b.BatchNumber == batchNumber))
                    .Execute()
                    .FirstOrDefault();
            
            return MapToModel(batch);
        }

        private MaterialBatchComponent MapToModel(IMaterialBatch entity)
        {
            if (entity == null)
            {
                return null;
            }

            var model = new MaterialBatchComponent(new MaterialBatchAdapter(entity, m_serviceLocator), this);
            return model;
        }

        public IEnumerable<MaterialBatchComponent> GetMaterialBatches(
            DateTime from,
            DateTime to,
            bool excludeCompositions,
            int? materialId,
            bool includeLocked = false,
            bool includeClosed = false,
            bool includeUnavailable = false)
        {
            var query = GetBatchQuery().Where(b => (b.Created >= from) && (b.Created <= to));
            
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

            if (!includeUnavailable)
            {
                query = query.Where(b => b.IsAvailable);
            }

            var entities = query.Execute()
                .Where(b => (!excludeCompositions) || (!b.Components.Any()))
                .Select(MapToModel);

            return entities;
        }

        public IEnumerable<int> GetBatchIds(DateTime @from, DateTime to, int? materialId, bool includeLocked = false,
            bool includeClosed = false, bool includeUnavailable = false)
        {
            return QueryBatches(query =>
            {
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

                if (!includeUnavailable)
                {
                    query = query.Where(b => b.IsAvailable);
                }
            }).Select(b => b.Id);
        }

        public MaterialBatchComponent SaveBottomLevelMaterialBatch(int id, IMaterial material, decimal amount, IMaterialUnit unit, string batchNr, DateTime receiveDt, decimal price, string invoiceNr, string supplierName, string currencySymbol, string variableSymbol)
        {
            if ((material.ProjectId != m_session.Project.Id) || (unit.ProjectId != m_session.Project.Id))
            {
                throw new InvalidOperationException("Illegal entity reference");
            }

            var m = m_materialRepository.GetMaterialById(material.Id);
            if (m.Components.Any())
            {
                throw new InvalidOperationException($"Materiál '{material.Name}' nelze naskladnit, protože se skládá z jiných materiálů. Použijte prosím funkci Výroba");
            }

            int? supplierId = null;
            if (!string.IsNullOrWhiteSpace(supplierName))
            {
                var supplier =
                    m_supplierRepository.GetSuppliers()
                        .FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.InvariantCultureIgnoreCase));

                if (supplier == null)
                {
                    throw new InvalidOperationException($"Nenalezen dodavatel \"{supplierName}\"");
                }

                supplierId = supplier.Id;
            }

            MaterialBatchComponent result;
            using (var tx = m_database.OpenTransaction())
            {
                IMaterialBatch entity;
                if (id > 0)
                {
                    throw new InvalidOperationException("UPDATE not implemented => Currency conversion");

                    entity =
                        GetBatchQuery()
                            .Where(b => (b.Id == id) && (b.ProjectId == m_session.Project.Id))
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
                    
                    var materialId = material.Id;

                    var existingBatchNr =
                        m_database.SelectFrom<IMaterialBatch>()
                            .Where(
                                b =>
                                    (b.ProjectId == m_session.Project.Id) && (b.MaterialId == materialId)
                                    && (b.BatchNumber == batchNr))
                            .Execute()
                            .FirstOrDefault();

                    if (existingBatchNr != null)
                    {
                        throw new InvalidOperationException("Již existuje šarže se zadaným číslem");
                    }

                }

                if (!m_conversionHelper.AreCompatible(material.NominalUnitId, unit.Id))
                {
                    throw new InvalidOperationException(
                              $"Nelze použít jednotku '{unit.Symbol}' pro materiál '{material.Name}' protože není převoditelná na nominální jednotku materiálu '{material.NominalUnit}'");
                }

                if ((material.RequiresPrice == true) && (Math.Abs(price) < 0.000001m))
                {
                    throw new InvalidOperationException("Cena je povinný údaj");
                }

                if ((material.RequiresInvoiceNr == true) && string.IsNullOrWhiteSpace(invoiceNr))
                {
                    throw new InvalidOperationException("Číslo faktury je povinný údaj");
                }

                if ((material.RequiresInvoiceNr == true) && string.IsNullOrWhiteSpace(variableSymbol))
                {
                    throw new InvalidOperationException("Var. symbol je povinný údaj");
                }

                if ((material.RequiresSupplierReference == true) && (supplierId == null))
                {
                    throw new InvalidOperationException("Dodavatel je povinný údaj");
                }

                entity.BatchNumber = batchNr;
                entity.Created = receiveDt;
                entity.MaterialId = material.Id;
                entity.Volume = amount;
                entity.UnitId = unit.Id;
                
                entity.Price = m_currencyConversionHelper.TryConvertToPrimaryCurrency(currencySymbol,
                    price,
                    c => entity.PriceConversionId = c.Id);
                
                entity.Note = string.Empty;
                entity.IsAvailable = true;
                entity.InvoiceNr = invoiceNr;
                entity.InvoiceVarSymbol = variableSymbol;
                entity.SupplierId = supplierId;
                
                m_database.Save(entity);

                result = GetBatchById(entity.Id);

                if (result.Components.Any())
                {
                    throw new InvalidOperationException($"Materiál '{material.Name}' nelze naskladnit, protože se skládá z jiných materiálů. Použijte prosím funkci Výroba");
                }

                m_materialBatchFacade.Value.ReleaseBatchAmountCache(result.Batch);

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

        public IEnumerable<IMaterialBatchComposition> GetCompositionsByComponentBatchId(int componentBatchId)
        {
            return
                m_database.SelectFrom<IMaterialBatchComposition>()
                    .Join(c => c.Unit)
                    .Where(c => c.ComponentId == componentBatchId)
                    .Execute();
        }

        public void UpdateBatchAvailability(int batchId, bool isAvailable)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var batch =
                    m_database.SelectFrom<IMaterialBatch>()
                        .Where(
                            b => (b.Id == batchId) && (b.IsAvailable != isAvailable) && (b.ProjectId == m_session.Project.Id))
                        .Execute()
                        .FirstOrDefault();

                if (batch != null)
                {
                    batch.IsAvailable = isAvailable;
                    m_database.Save(batch);
                }

                tx.Commit();
            }
        }

        public MaterialBatchComponent CreateProductionBatch(int materialId,
            string batchNumber,
            decimal amount,
            IMaterialUnit unit,
            decimal productionWorkPrice)
        {
            if (string.IsNullOrWhiteSpace(batchNumber))
            {
                throw new InvalidOperationException("Musí být číslo šarže");
            }

            if (unit == null)
            {
                throw new InvalidOperationException("Musí být vybrána měrná jednotka");
            }
            
            var material = m_materialRepository.GetMaterialById(materialId);
            if (material == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            if (!m_conversionHelper.AreCompatible(material.Adaptee.NominalUnitId, unit.Id))
            {
                throw new InvalidOperationException($"Požadovaná měrná jednotka \"{unit.Symbol}\" není platná pro materiál \"{material.Name}\" protože není převoditelná na nominální jednotku materiálu \"{material.NominalUnit.Symbol}\" ");
            }

            var alreadyExisting = GetMaterialBatches(
                DateTime.Now.AddYears(-100),
                DateTime.Now.AddYears(100),
                false,
                materialId,
                true,
                true,
                true).FirstOrDefault(m => m.Batch.BatchNumber.Equals(batchNumber, StringComparison.InvariantCultureIgnoreCase));
            if (alreadyExisting != null)
            {
                throw new InvalidOperationException($"Již existuje šarže {batchNumber} materiálu {material.Name}");
            }

            var entity = m_database.New<IMaterialBatch>();
            entity.BatchNumber = batchNumber;
            entity.UnitId = unit.Id;
            entity.AuthorId = m_session.User.Id;
            entity.Created = DateTime.Now;
            entity.ProjectId = m_session.Project.Id;
            entity.MaterialId = material.Id;
            entity.Volume = amount;
            entity.Note = string.Empty;
            entity.ProductionWorkPrice = productionWorkPrice;

            m_database.Save(entity);

            return GetBatchById(entity.Id);
        }

        public void MarkBatchAllProductionStepsDone(int batchId)
        {
            var batch = GetBatchById(batchId);
            batch.Batch.AllStepsDone = true;

            m_database.Save(batch.Batch);
        }

        public string GetBatchNumberById(int batchId)
        {
            return m_cache.ReadThrough($"batchNrById_{m_session.Project.Id}_{batchId}",
                TimeSpan.FromHours(24),
                () =>
                {
                    return
                        m_database.SelectFrom<IMaterialBatch>()
                            .Where(b => (b.Id == batchId) && (b.ProjectId == m_session.Project.Id))
                            .Take(1)
                            .Execute()
                            .FirstOrDefault()?.BatchNumber;
                });
        }

        public int? GetBatchIdByNumber(int materialId, string batchNumber)
        {
            return m_cache.ReadThrough($"batchIdByNr_{materialId}_{batchNumber}",
                TimeSpan.FromHours(24),
                () =>
                    m_database.SelectFrom<IMaterialBatch>()
                        .Where(b => b.ProjectId == m_session.Project.Id)
                        .Where(b => b.BatchNumber == batchNumber)
                        .Where(b => b.MaterialId == materialId)
                        .Take(1)
                        .Execute()
                        .FirstOrDefault()?.Id);
        }

        public IEnumerable<int> QueryBatchIds(Action<IQueryBuilder<IMaterialBatch>> customize)
        {
            var qry = m_database.SelectFrom<IMaterialBatch>().Where(b => b.ProjectId == m_session.Project.Id);

            customize(qry);

            return qry.Execute().Select(b => b.Id).Distinct();
        }

        public IEnumerable<IMaterialBatch> QueryBatches(Action<IQueryBuilder<IMaterialBatch>> customize)
        {
            var qry = m_database.SelectFrom<IMaterialBatch>().Where(b => b.ProjectId == m_session.Project.Id);

            customize(qry);

            return qry.Execute();
        }

        public MaterialBatchComponent UpdateBatch(int id, Action<IMaterialBatchEditables> edit)
        {
            var batch = GetBatchById(id).Ensure();

            edit(batch.Batch);

            m_database.Save(batch.Batch);

            return GetBatchById(id);
        }

        public IEnumerable<IBatchProductionStep> GetPerformedSteps(int batchId)
        {
            return m_database.SelectFrom<IBatchProductionStep>()
                .Join(s => s.Batch)
                .Join(s => s.Step)
                .Join(s => s.SourceBatches)
                .Join(s => s.SourceBatches.Each().Unit)
                .Join(s => s.SourceBatches.Each().SourceBatch)
                .Join(s => s.SourceBatches.Each().SourceBatch.Unit)
                .Join(s => s.SourceBatches.Each().SourceBatch.Material)
                .Where(s => s.BatchId == batchId)
                .Where(s => s.Batch.ProjectId == m_session.Project.Id)
                .Execute();
        }

        public IEnumerable<MaterialBatchComponent> GetBatchesByComponentInventory(int componentMaterialInventoryId, int compositionYear, int compositionMonth)
        {
            var from = new DateTime(compositionYear, compositionMonth, 1).Date;
            var to = from.AddMonths(1);

            var compositionIds = m_database.SelectFrom<IMaterialBatch>()
                .Join(b => b.Material)
                .Join(b => b.Components)
                .Join(b => b.Components.Each().Component)
                .Join(b => b.Components.Each().Component.Material)
                .Where(b => b.ProjectId == m_session.Project.Id)
                .Where(b => b.Created >= from && b.Created < to)
                .Where(b => b.Components.Each().Component.Material.InventoryId == componentMaterialInventoryId)
                .Execute().Select(r => r.Id).Distinct();

            foreach (var compositionBatchId in compositionIds)
            {
                yield return GetBatchById(compositionBatchId);
            }
        }

        public IEnumerable<int> GetBatchesByProductionStepComponentInventory(int stepComponentInventory,
            int compositionYear,
            int compositionMonth, bool includeBatchesHiddenForAccounting)
        {
            var from = new DateTime(compositionYear, compositionMonth, 1).Date;
            var to = from.AddMonths(1);

            var query = m_database.SelectFrom<IBatchProductionStep>()
                .Join(s => s.SourceBatches)
                .Join(s => s.SourceBatches.Each().SourceBatch)
                .Join(s => s.SourceBatches.Each().SourceBatch.Material)
                .Where(s => s.SourceBatches.Each().SourceBatch.ProjectId == m_session.Project.Id)
                .Where(s => s.SourceBatches.Each().SourceBatch.Material.InventoryId == stepComponentInventory)
                .Where(s => s.ConfirmDt >= from && s.ConfirmDt < to);

            if (!includeBatchesHiddenForAccounting)
            {
                query = query.Where(s => s.SourceBatches.Each().SourceBatch.IsHiddenForAccounting != true);
            }
                
            return query.Execute().Select(s => s.BatchId).Distinct()
                .ToList();
        }

        public IEnumerable<IMaterialBatchComposition> GetBatchComponents(int compositionId)
        {
            return m_database.SelectFrom<IMaterialBatchComposition>().Where(c => c.CompositionId == compositionId)
                .Execute().Select(i => new MaterialBatchCompositionAdapter(m_serviceLocator, i));
        }

        private IQueryBuilder<IMaterialBatch> GetBatchQuery()
        {
            return
                m_database.SelectFrom<IMaterialBatch>()
                    .Where(b => b.ProjectId == m_session.Project.Id);
        }

        public Tuple<int, string> GetBatchNumberAndMaterialIdByBatchId(int batchId)
        {
            return m_cache.ReadThrough($"batchkey_{batchId}", TimeSpan.FromHours(1), () =>
            {
                var batch = GetBatchById(batchId).Ensure();

                return new Tuple<int, string>(batch.Batch.MaterialId, batch.Batch.BatchNumber);
            });
        }
    }
}
