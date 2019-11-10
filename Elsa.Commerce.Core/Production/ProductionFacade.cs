using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Production.Model;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Production
{
    public class ProductionFacade : IProductionFacade
    {
        private readonly IDatabase m_database;
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitConversionHelper m_unitConversion;
        private readonly AmountProcessor m_amountProcessor;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly ILog m_log;
        private readonly IUnitRepository m_unitRepository;
        private readonly ISession m_session;

        public ProductionFacade(
            IDatabase database,
            IMaterialBatchRepository batchRepository,
            IMaterialRepository materialRepository,
            IUnitConversionHelper unitConversion, 
            AmountProcessor amountProcessor, 
            ILog log, 
            IMaterialBatchFacade batchFacade, IUnitRepository unitRepository, ISession session)
        {
            m_database = database;
            m_batchRepository = batchRepository;
            m_materialRepository = materialRepository;
            m_unitConversion = unitConversion;
            m_amountProcessor = amountProcessor;
            m_log = log;
            m_batchFacade = batchFacade;
            m_unitRepository = unitRepository;
            m_session = session;
        }

        public ProductionBatchModel GetProductionBatch(int batchId)
        {
            return LoadAndValidateBatchModel(batchId);
        }

        public ProductionBatchModel CreateOrUpdateProductionBatch(int? batchId,
            int materialId,
            string batchNumber,
            decimal amount,
            IMaterialUnit unit,
            decimal productionWorkPrice)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var topMaterial = m_materialRepository.GetMaterialById(materialId);
                if (topMaterial == null)
                {
                    throw new InvalidOperationException("Invalid Material entity reference");
                }

                MaterialBatchComponent batchEntity;
                if (batchId == null)
                {
                    batchEntity = m_batchRepository.CreateProductionBatch(materialId, batchNumber, amount, unit, productionWorkPrice);
                }
                else
                {
                    batchEntity = m_batchRepository.GetBatchById(batchId.Value);
                    if (batchEntity == null)
                    {
                        throw new InvalidOperationException("Invalid batch reference");
                    }

                    if (batchEntity.Batch.MaterialId != materialId)
                    {
                        foreach (var component in batchEntity.Batch.Components)
                        {
                            m_batchFacade.UnassignComponent(batchEntity.Batch.Id, component.ComponentId);
                        }

                        batchEntity.Batch.MaterialId = materialId;
                        batchEntity.Batch.UnitId = topMaterial.Adaptee.NominalUnitId;

                        m_database.Save(batchEntity.Batch);

                        batchEntity = m_batchRepository.GetBatchById(batchId.Value);
                    }
                }
                
                batchEntity.Batch.BatchNumber = batchNumber;
                batchEntity.Batch.Note = string.Empty;

                if (batchEntity.Components.Any())
                {
                    if ((Math.Abs(amount - batchEntity.ComponentAmount) > 0m) || (batchEntity.ComponentUnit.Id != unit.Id))
                    {
                        throw new InvalidOperationException("Nelze změnit množství či jednotku šarže, která má přiřazeny materiály");
                    }
                }

                if (!m_unitConversion.AreCompatible(topMaterial.Adaptee.NominalUnitId, unit.Id))
                {
                    throw new InvalidOperationException($"Pro materiál \"{topMaterial.Name}\" nelze použít jednotku \"{unit.Symbol}\" ");
                }
                
                batchEntity.Batch.UnitId = unit.Id;
                batchEntity.Batch.Volume = amount;

                m_database.Save(batchEntity.Batch);

                var result = LoadAndValidateBatchModel(batchEntity.Batch.Id);

                var autoassigned = false;
                foreach (var componentToBeAutoresolved in result.Components.Where(c => c.Assignments.All(a => a.UsedBatchId == null)))
                {
                    var material = m_materialRepository.GetMaterialById(componentToBeAutoresolved.MaterialId);
                    if (!material.AutomaticBatches)
                    {
                        continue;
                    }

                    var resolutions = m_batchFacade.AutoResolve(
                        componentToBeAutoresolved.MaterialId,
                        new Amount(componentToBeAutoresolved.RequiredAmount, m_unitRepository.GetUnitBySymbol(componentToBeAutoresolved.RequiredAmountUnitSymbol)));

                    foreach (var resolution in resolutions)
                    {
                        autoassigned = true;
                        m_batchFacade.AssignComponent(result.BatchId, resolution.Item1.Id, resolution.Item2);
                    }
                }

                if (autoassigned)
                {
                    result = LoadAndValidateBatchModel(batchEntity.Batch.Id);
                }
                
                tx.Commit();

                return result;
            }
        }

        public ProductionBatchModel SetComponentSourceBatch(
            int? materialBatchCompositionId,
            int productionBatchId,
            int sourceBatchId,
            decimal usedAmount,
            string usedAmountUnitSymbol)
        {
            using (var tx = m_database.OpenTransaction())
            {
                if (materialBatchCompositionId != null)
                {
                    var composition =
                        m_database.SelectFrom<IMaterialBatchComposition>()
                            .Where(
                                c => (c.CompositionId == productionBatchId) && (c.Id == materialBatchCompositionId.Value))
                            .Execute()
                            .FirstOrDefault();

                    if (composition != null)
                    {
                        m_batchFacade.UnassignComponent(productionBatchId, composition.ComponentId);
                    }
                }

                var unit = m_unitRepository.GetUnitBySymbol(usedAmountUnitSymbol);
                if (unit == null)
                {
                    throw new InvalidOperationException($"Neznámá měrná jednotka \"{usedAmountUnitSymbol}\"");
                }

                var childBatch = m_batchRepository.GetBatchById(sourceBatchId);
                if ((childBatch == null) || childBatch.IsClosed || childBatch.IsLocked || !childBatch.Batch.IsAvailable)
                {
                    throw new InvalidOperationException("Požadovaná šarže nedostupná");
                }

                ProductionBatchModel parentBatch;
                ProductionBatchComponentModel targetComponent;
                while (true)
                {
                    parentBatch = LoadAndValidateBatchModel(productionBatchId);
                    if (parentBatch == null)
                    {
                        throw new InvalidOperationException("Šarže nedostupná");
                    }

                    targetComponent = parentBatch.Components.FirstOrDefault(c => c.MaterialId == childBatch.Batch.MaterialId);
                    if (targetComponent == null)
                    {
                        throw new InvalidOperationException(
                                  $"Složení {parentBatch.MaterialName} neobsahuje {childBatch.Batch.Material.Name}");
                    }

                    var concurrentAssignment = targetComponent.Assignments.FirstOrDefault(a => (a.UsedBatchId != null) && (a.UsedBatchId == childBatch.Batch.Id));
                    if (concurrentAssignment != null)
                    {
                        m_batchFacade.UnassignComponent(productionBatchId, concurrentAssignment.UsedBatchId.Value);

                        var inProcUnit =
                            m_unitConversion.ConvertAmount(
                                m_unitRepository.GetUnitBySymbol(concurrentAssignment.UsedAmountUnitSymbol).Id,
                                unit.Id,
                                concurrentAssignment.UsedAmount);

                        usedAmount += inProcUnit;
                        continue;
                    }

                    break;
                }

                if (!m_unitConversion.AreCompatible(childBatch.Batch.UnitId, unit.Id))
                {
                    throw new InvalidOperationException($"Nelze použít měrnou jednotku \"{usedAmountUnitSymbol}\" pro material {childBatch.Batch.Material.Name}");
                }

                var batchPlaceholder = targetComponent.Assignments.SingleOrDefault(a => a.UsedBatchId == null);
                if (batchPlaceholder == null)
                {
                    throw new InvalidOperationException($"Potřebné množství materiálu \"{childBatch.Batch.Material.Name}\" již bylo přiřazeno");
                }

                var batchAvailableAmount = m_batchFacade.GetAvailableAmount(sourceBatchId);

                if (!batchAvailableAmount.IsPositive)
                {
                    throw new InvalidOperationException($"Šarže {childBatch.Batch.BatchNumber} je již plně spotřebována");
                }

                var placeholderUnit = m_unitRepository.GetUnitBySymbol(batchPlaceholder.UsedAmountUnitSymbol);

                var usedAmountAmount = m_unitConversion.ConvertAmount(new Amount(usedAmount, unit), placeholderUnit.Id);
                var placeholdlerAmount = new Amount(batchPlaceholder.UsedAmount, placeholderUnit);
                var availabAmount = m_unitConversion.ConvertAmount(batchAvailableAmount, placeholderUnit.Id);

                var amountToAllocate = m_unitConversion.ConvertAmount(m_amountProcessor.Min(availabAmount, usedAmountAmount, placeholdlerAmount), placeholderUnit.Id);

                m_batchFacade.AssignComponent(parentBatch.BatchId, childBatch.Batch.Id, amountToAllocate);

                var result = LoadAndValidateBatchModel(productionBatchId);

                tx.Commit();

                return result;
            }
        }

        public ProductionBatchModel RemoveComponentSourceBatch(int productionBatchId, int sourceBatchId)
        {
            using (var tx = m_database.OpenTransaction())
            {
                m_batchFacade.UnassignComponent(productionBatchId, sourceBatchId);

                var result = LoadAndValidateBatchModel(productionBatchId);
                tx.Commit();

                return result;
            }
        }

        public IEnumerable<IMaterialBatch> LoadProductionBatches(long? fromDt, int pageSize)
        {
            var query =
                m_database.SelectFrom<IMaterialBatch>()
                    .Join(b => b.Material)
                    .Join(b => b.Unit)
                    .Join(b => b.Author)
                    .Where(b => b.ProjectId == m_session.Project.Id)
                    .OrderByDesc(b => b.Created)
                    .Take(pageSize);

            if (fromDt != null)
            {
                var d = new DateTime(fromDt.Value);

                query = query.Where(b => b.Created < d);
            }

            return query.Execute();
        }

        private ProductionBatchModel LoadAndValidateBatchModel(int productionBatchId)
        {
            var complete = true;

            var topBatch = m_batchRepository.GetBatchById(productionBatchId)?.Batch;
            if (topBatch == null)
            {
                return null;
            }

            var topMaterial = m_materialRepository.GetMaterialById(topBatch.MaterialId);
            if (topMaterial == null)
            {
                throw new InvalidOperationException("Aktuální šarže odkazuje na nedostupný materiál");
            }

            var commonUnit = m_unitConversion.GetPrefferedUnit(topMaterial.NominalUnit, topBatch.Unit);

            var batchAmountInCommonUnit = m_unitConversion.ConvertAmount(
                topBatch.UnitId,
                commonUnit.Id,
                topBatch.Volume);

            var topMaterialNominalAmountInCommonUnit = m_unitConversion.ConvertAmount(
                topMaterial.NominalUnit.Id,
                commonUnit.Id,
                topMaterial.NominalAmount);

            var componentFactor = batchAmountInCommonUnit / topMaterialNominalAmountInCommonUnit;

            var model = new ProductionBatchModel
                            {
                                BatchNumber = topBatch.BatchNumber,
                                BatchId = topBatch.Id,
                                ProducedAmount = topBatch.Volume,
                                ProducedAmountUnitSymbol = topBatch.Unit.Symbol,
                                MaterialId = topMaterial.Id,
                                MaterialName = topMaterial.Name,
                                IsLocked = topBatch.LockDt != null,
                                ProductionWorkPrice = topBatch.ProductionWorkPrice ?? 0
                            };

            if (string.IsNullOrWhiteSpace(model.BatchNumber))
            {
                complete = false;
            }

            foreach (var requiredComponent in topMaterial.Components)
            {
                var componentModel = new ProductionBatchComponentModel
                                         {
                                             MaterialName = requiredComponent.Material.Name,
                                             MaterialId = requiredComponent.Material.Id,
                                             RequiredAmount = requiredComponent.Amount * componentFactor,
                                             RequiredAmountUnitSymbol = requiredComponent.Unit.Symbol
                                         };

                model.Components.Add(componentModel);

                var amountToResolve = new Amount(componentModel.RequiredAmount, requiredComponent.Unit);

                var subbatches = topBatch.Components.Where(c => c.Component.MaterialId == requiredComponent.Material.Id).ToList();

                foreach (var subBatch in subbatches)
                {
                    var appliedAmount = new Amount(subBatch.Volume, subBatch.Unit);
                    amountToResolve = m_amountProcessor.Subtract(amountToResolve, appliedAmount);

                    var subBatchModel = new SubBatchAssignmentModel
                                            {
                                                UsedAmount = appliedAmount.Value,
                                                UsedAmountUnitSymbol = appliedAmount.Unit.Symbol,
                                                UsedBatchNumber = subBatch.Component.BatchNumber,
                                                UsedBatchId = subBatch.Component.Id,
                                                MaterialBatchCompositionId = subBatch.Id
                                            };

                    componentModel.Assignments.Add(subBatchModel);
                }

                if (amountToResolve.IsNegative)
                {
                    m_log.Error($"PROBLEM - productionBatchId={productionBatchId}, topMaterial={topMaterial.Name}, requiredComponent.MaterialId={requiredComponent.Material.Name}, amountToResolve={amountToResolve}");
                    //if (!subbatches.Any())
                   // {
                        m_log.Error("FATAL - we have no batches assigned, but remaining amount is already < 0");
                        throw new InvalidOperationException("Došlo k fatální chybě, kontaktujte podporu");
                   // }

                    //return RemoveComponentSourceBatch(topBatch.Id, subbatches.Last().Id);
                }
                else if (amountToResolve.IsPositive)
                {
                    var aditionalSubbatch = new SubBatchAssignmentModel()
                                                {
                                                    UsedAmount = amountToResolve.Value,
                                                    UsedAmountUnitSymbol = amountToResolve.Unit.Symbol
                                                };
                    complete = false;
                    componentModel.Assignments.Add(aditionalSubbatch);
                }
            }

            model.IsComplete = complete;

            if (topBatch.IsAvailable != complete)
            {
                m_batchRepository.UpdateBatchAvailability(topBatch.Id, complete);
            }
            
            return model;
        }
    }
}

