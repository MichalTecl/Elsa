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
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

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

        public ProductionBatchModel CreateOrUpdateProductionBatch(
            int? batchId,
            int materialId,
            string batchNumber,
            decimal amount,
            IMaterialUnit unit)
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
                    batchEntity = m_batchRepository.CreateProductionBatch(materialId, batchNumber, amount, unit);
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

                var amountToAllocate =
                    m_amountProcessor.Min(
                        m_amountProcessor.Min(
                            batchAvailableAmount,
                            new Amount(
                                batchPlaceholder.UsedAmount,
                                m_unitRepository.GetUnitBySymbol(batchPlaceholder.UsedAmountUnitSymbol))),
                        new Amount(usedAmount, unit));

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

        public IEnumerable<IMaterialBatch> FindBatchesWithUnresolvedProductionSteps(string query)
        {
            var select = GetBatchesWithUnresolvedProductionStepsQuery();

            if (string.IsNullOrWhiteSpace(query))
            {
                select = select.Where(m => m.Material.AutomaticBatches);
            }
            else
            {
                select = select.Where(m => m.BatchNumber.Like($"%{query}"));
            }

            return select.Execute();
        }

        private IQueryBuilder<IMaterialBatch> GetBatchesWithUnresolvedProductionStepsQuery()
        {
            var select = m_database.SelectFrom<IMaterialBatch>()
                .Join(m => m.Material.Steps)
                .Join(m => m.Unit)
                .Join(m => m.Author)
                .Join(m => m.Material.NominalUnit)
                .Join(m => m.PerformedSteps)
                .Where(m => m.Material.Steps.Each().Id != null)
                .Where(m => m.ProjectId == m_session.Project.Id)
                .Where(m => !(m.AllStepsDone ?? false))
                .OrderByDesc(m => m.Created);

            return select;
        }

        public IEnumerable<ProductionStepViewModel> GetStepsToProceed(int? materialBatchId, int materialId, bool skipComponents = false)
        {
            var batchesQuery = GetBatchesWithUnresolvedProductionStepsQuery();

            batchesQuery = batchesQuery.Where(b => b.MaterialId == materialId);

            if (materialBatchId != null)
            {
                batchesQuery = batchesQuery.Where(b => b.Id == materialBatchId.Value);
            }

            var batches = batchesQuery.Execute().ToList();

            var result = new List<ProductionStepViewModel>();

            if (!batches.Any())
            {
                return result;
            }

            foreach (var batch in batches)
            {
                var requiredSteps = m_materialRepository.GetMaterialProductionSteps(batch.MaterialId).Ordered().ToList();
                if (requiredSteps.Count == 0)
                {
                    continue;
                }

                var maxStepRemainingAmount = new Amount(batch);

                foreach (var requiredStep in requiredSteps)
                {
                    // if some previous step is not resolved, we cannot offer this one
                    if (!maxStepRemainingAmount.IsPositive)
                    {
                        break;
                    }

                    // let's calculate how much was already done
                    var amountResolvedForThisStep = new Amount(0, batch.Unit);
                    foreach (var passedStep in batch.PerformedSteps.Where(s => s.StepId == requiredStep.Id))
                    {
                        amountResolvedForThisStep = m_amountProcessor.Add(amountResolvedForThisStep,
                            new Amount(passedStep.ProducedAmount, batch.Unit));
                    }

                    // MIN(BATCH_VOLUME, PREVIOUS_STEPS_VOLUME) - ALREADY_DONE is max volume we can request for this particular step
                    var remainingAmountForThisStep = m_amountProcessor.Subtract(maxStepRemainingAmount, amountResolvedForThisStep);
                    if (!remainingAmountForThisStep.IsPositive)
                    {
                        // If we cannot produce more
                        continue;
                    }

                    var model = new ProductionStepViewModel()
                    {
                        MaterialProductionStepId = requiredStep.Id,
                        Quantity = remainingAmountForThisStep.Value,
                        MaxQuantity = remainingAmountForThisStep.Value,
                        StepName = requiredStep.Name,
                        RequiresPrice = requiredStep.RequiresPrice,
                        RequiresTime = requiredStep.RequiresSpentTime,
                        RequiresWorkerReference = requiredStep.RequiresWorkerReference,
                        MaterialName = batch.Material.Name,
                        MaterialId = batch.MaterialId,
                        BatchNumber = batch.BatchNumber,
                        UnitSymbol = batch.Unit.Symbol,
                        IsAutoBatch = batch.Material.AutomaticBatches,
                        BatchMaterial = batch.Material,
                        MaterialStep = requiredStep,
                        Unit = batch.Unit
                    };

                    model.BatchIds.Add(batch.Id);

                    // next steps could be max what was already done in this step
                    maxStepRemainingAmount = m_amountProcessor.Min(maxStepRemainingAmount, remainingAmountForThisStep);

                    result.Add(model);
                }
            }

            result = ProductionStepViewModel.JoinAutomaticMaterials(result).ToList();

            if (!skipComponents)
            {
                foreach (var sumStep in result)
                {
                    PopulateWithMaterials(sumStep);
                }
            }

            return result;
        }

        public ProductionStepViewModel UpdateProductionStep(ProductionStepViewModel model)
        {
            IExtendedMaterialModel material = null;
            if (model.MaterialId > 0)
            {
                material = m_materialRepository.GetMaterialById(model.MaterialId);
            }
            else if (!string.IsNullOrWhiteSpace(model.MaterialName))
            {
                material = m_materialRepository.GetMaterialByName(model.MaterialName);
            }
            else if (model.BatchIds?.Count == 1)
            {
                var batch = m_batchRepository.GetBatchById(model.BatchIds.Single());
                material = m_materialRepository.GetMaterialById(batch.Batch.MaterialId);
            }

            var materialId = material?.Id ?? -1;
            if ((materialId < 1) || (material == null))
            {
                throw new InvalidOperationException($"Neznamy material \"{model.MaterialName}\"");
            }

            model.IsAutoBatch = material.AutomaticBatches;
            model.MaterialId = material.Id;
            model.MaterialName = material.Name;
            model.BatchMaterial = material.Adaptee;
            
            if (!model.BatchIds.Any() && (!material.AutomaticBatches))
            {
                if (string.IsNullOrWhiteSpace(model.BatchNumber))
                {
                    model.NeedsBatchNumber = true;
                }
                else
                {
                    ResolveBatch(model);
                }
            }

            int? batchId = null;
            if (model.BatchIds?.Count == 1)
            {
                batchId = model.BatchIds.Single();
            }

            var sourceSteps = GetStepsToProceed(batchId, materialId, true).ToList();
            var sourceStep = sourceSteps.FirstOrDefault(srs => srs.IsSameStep(model));

            if ((batchId != null) && (sourceStep == null))
            {
                throw new InvalidOperationException("Pro požadovanou šarži není možné vložit tento výrobní krok");
            }

            if ((sourceStep == null) && model.NeedsBatchNumber)
            {
                if (model.NeedsBatchNumber)
                {
                    var templateStep = sourceSteps.FirstOrDefault(i => i.MaterialProductionStepId == model.MaterialProductionStepId);
                    if (templateStep != null)
                    {
                        model.StepName = templateStep.StepName;
                    }

                    return model;
                }
                else
                {
                    throw new InvalidOperationException("Invalid request");
                }
            } 

            if (model.Quantity > sourceStep.Quantity)
            {
                throw new InvalidOperationException($"Nedovolene mnozstvi {model.Quantity}. Maximum = {sourceStep.Quantity}");
            }

            sourceStep.Quantity = model.Quantity;
            sourceStep.Materials.Clear();
            sourceStep.Materials.AddRange(model.Materials);

            PopulateWithMaterials(sourceStep);

            sourceStep.Worker = model.Worker;
            sourceStep.Hours = model.Hours;
            sourceStep.Price = model.Price;

            if (sourceStep.RequiresPrice && (!((sourceStep.Price ?? 0) > 0)))
            {
                sourceStep.IsValid = false;
            }

            if (sourceStep.RequiresTime && (!((sourceStep.Hours ?? 0) > 0)))
            {
                sourceStep.IsValid = false;
            }

            if (sourceStep.RequiresWorkerReference && string.IsNullOrWhiteSpace(sourceStep.Worker?.Trim()))
            {
                sourceStep.IsValid = false;
            }

            return sourceStep;
        }

        private void ResolveBatch(ProductionStepViewModel model)
        {
            var batch = m_batchFacade.FindBatchBySearchQuery(model.MaterialId, model.BatchNumber);
            if (batch == null)
            {
                throw new InvalidOperationException($"Nenalezena sarze \"{model.BatchNumber}\"");
            }

            model.BatchIds.Add(batch.Id);
        }

        public void SaveProductionStep(ProductionStepViewModel model)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var step = UpdateProductionStep(model);
                if (!step.IsValid)
                {
                    throw new InvalidOperationException("Nektere povinne hodnoty nejsou vyplneny");
                }

                var material = m_materialRepository.GetMaterialById(model.MaterialId);
                if (material == null)
                {
                    throw new InvalidOperationException("Chybna reference materialu");
                }

                var totalProducedAmount = new Amount(model.Quantity, m_unitRepository.GetUnitBySymbol(model.UnitSymbol));
            
                foreach (var targetBatchId in model.BatchIds)
                {
                    var targetStep =
                        GetStepsToProceed(targetBatchId, model.MaterialId, false)
                            .FirstOrDefault(s => s.MaterialProductionStepId == model.MaterialProductionStepId);
                    
                    if(targetStep == null) { throw new InvalidOperationException("Krok nenalezen");}
                    if (model.Quantity > targetStep.MaxQuantity) { throw new InvalidOperationException("Neni mozne zpracovat zadane mnozstvi");}

                    var amountInThisBatch = m_amountProcessor.Min(totalProducedAmount,
                        new Amount(targetStep.Quantity, targetStep.Unit));
                    
                    var stepEntity = m_database.New<IBatchProductionStep>();
                    stepEntity.BatchId = targetBatchId;
                    stepEntity.ConfirmDt = DateTime.Now;
                    stepEntity.Price = model.Price;
                    stepEntity.ProducedAmount = amountInThisBatch.Value;
                    stepEntity.SpentHours = model.Hours;
                    stepEntity.StepId = targetStep.MaterialProductionStepId;
                    stepEntity.ConfirmUserId = m_session.User.Id;
                    stepEntity.ProducedAmount = amountInThisBatch.Value;
                    
                    m_database.Save(stepEntity);

                    var materials = step.ReleaseMaterialForAmount(amountInThisBatch.Value).ToList();

                    foreach (var matEntry in materials)
                    {
                        var entryAmount = new Amount(matEntry.Amount, m_unitRepository.GetUnitBySymbol(matEntry.UnitSymbol));
                        var componentEntity = m_database.New<IBatchProuctionStepSourceBatch>();
                        componentEntity.StepId = stepEntity.Id;
                        componentEntity.UnitId = entryAmount.Unit.Id;

                        var batch = m_batchFacade.FindBatchBySearchQuery(matEntry.MaterialId, matEntry.BatchNumber);
                        var availableBatchAmount = m_batchFacade.GetAvailableAmount(batch.Id);

                        if (m_amountProcessor.GreaterThan(entryAmount, availableBatchAmount))
                        {
                            throw new InvalidOperationException($"Vstupní šarže \"{batch.BatchNumber}\" neobsahuje potřebné množství materiálu \"{matEntry.MaterialName}\"");
                        }

                        componentEntity.UsedAmount = matEntry.Amount;
                        componentEntity.SourceBatchId = batch.Id;

                        m_database.Save(componentEntity);
                        m_batchFacade.ReleaseBatchAmountCache(batch);
                    }

                    var targetBatch = m_batchRepository.GetBatchById(targetBatchId).Batch;

                    m_batchFacade.ReleaseBatchAmountCache(targetBatch);
                    
                    TryMarkBatchStepsComplete(targetBatch);
                }

                tx.Commit();
            }
        }

        public bool CheckProductionStepCanBeDeleted(IMaterialBatchStatus batchStatus, int stepToDelete, IMaterialBatch batch)
        {
            if (batchStatus.CalculateAvailableAmount(m_amountProcessor, stepToDelete).IsNegative)
            {
                return false;
            }

            var requiredSteps = batchStatus.RequiredSteps.Ordered().Reverse().Select(s => s.Id).ToList();

            if (requiredSteps.Count < 2)
            {
                //There is only this required step, so npot any other step(s) could be underflowed by deletion of this step
                return true;
            }

            var followingStepAmount = new Amount(0m, batch.Unit);

            var testedStepFound = false;
            foreach (var requiredStep in requiredSteps)
            {
                var resolvedAmount = new Amount(0m, followingStepAmount.Unit);

                foreach (var performedStep in batchStatus.ResolvedSteps.Where(s => s.StepId == requiredStep))
                {
                    if (performedStep.Id == stepToDelete)
                    {
                        testedStepFound = true;
                    }
                    else
                    {
                        resolvedAmount = m_amountProcessor.Add(resolvedAmount, new Amount(performedStep.ProducedAmount, resolvedAmount.Unit));
                    }
                }

                if (m_amountProcessor.GreaterThan(followingStepAmount, resolvedAmount))
                {
                    return false;
                }

                if (testedStepFound)
                {
                    return true;
                }

                followingStepAmount = resolvedAmount;

            }

            return true;
        }

        public void DeleteProductionStep(int productionStepId)
        {
            using (var tx = m_database.OpenTransaction())
            {

                var step =
                    m_database.SelectFrom<IBatchProductionStep>()
                        .Join(s => s.Batch)
                        .Join(s => s.SourceBatches)
                        .Join(s => s.SourceBatches.Each().SourceBatch)
                        .Where(s => s.Id == productionStepId)
                        .Execute()
                        .ToList()
                        .FirstOrDefault();

                if ((step == null) || (step.Batch?.ProjectId != m_session.Project.Id))
                {
                    throw new InvalidOperationException("Invalid entity reference");
                }

                var batch = m_batchRepository.GetBatchById(step.BatchId);
                var batchStatus = m_batchFacade.GetBatchStatus(step.BatchId);

                if ((batch == null) || (batchStatus == null))
                {
                    throw new InvalidOperationException("Invalid entity reference");
                }

                if (!CheckProductionStepCanBeDeleted(batchStatus, productionStepId, batch.Batch))
                {
                    throw new InvalidOperationException("Operation not allowed");
                }

                m_batchFacade.ReleaseBatchAmountCache(batch.Batch);

                if (batch.Batch.AllStepsDone == true)
                {
                    batch.Batch.AllStepsDone = false;
                    m_database.Save(batch);
                }

                foreach (var sourceBatch in step.SourceBatches)
                {
                    m_database.Delete(sourceBatch);
                    m_batchFacade.ReleaseBatchAmountCache(sourceBatch.SourceBatch);
                }

                m_database.Delete(step);
                
                tx.Commit();
            }
        }

        private void TryMarkBatchStepsComplete(IMaterialBatch batch)
        {
            if (!GetStepsToProceed(batch.Id, batch.MaterialId, true).Any())
            {
                m_batchRepository.MarkBatchAllProductionStepsDone(batch.Id);
            }
        }

        private void PopulateWithMaterials(ProductionStepViewModel model)
        {
            if (model.Quantity <= 0)
            {
                model.IsValid = false;
                return;
            }

            var batchIndex = new List<IMaterialBatch>();

            var currentMats = model.Materials.Where(m => !string.IsNullOrWhiteSpace(m.BatchNumber?.Trim()) && !m.AutomaticBatches).ToList();
            model.Materials.Clear();

            foreach (var i in currentMats)
            {
                var batch = m_batchFacade.FindBatchBySearchQuery(i.MaterialId, i.BatchNumber);
                i.BatchNumber = batch.BatchNumber;
                batchIndex.Add(batch);
            }
            
            foreach (var materialId in currentMats.Select(m => m.MaterialId).Distinct())
            {
                foreach (var batchNr in currentMats.Where(m => m.MaterialId == materialId).Select(m => m.BatchNumber).Distinct())
                {
                    var fragments =
                        currentMats.Where(f => (f.MaterialId == materialId) && (f.BatchNumber == batchNr)).ToList();

                    if (!fragments.Any())
                    {
                        continue;
                    }

                    model.Materials.Add(new MaterialBatchResolutionModel
                    {
                        UnitSymbol = fragments[0].UnitSymbol,
                        BatchNumber = fragments[0].BatchNumber,
                        MaterialId = fragments[0].MaterialId,
                        MaterialName = fragments[0].MaterialName,
                        Amount = fragments.Sum(f => f.Amount)
                    });
                }
            }

            
           
            var nominalMaterialAmount = new Amount(model.BatchMaterial.NominalAmount, model.BatchMaterial.NominalUnit);

            foreach (var component in model.MaterialStep.Components)
            {
                //for 1kg of material we need 500g of this composition => For 10kg batch it's 5kg of composition
                var stepAmount = m_amountProcessor.LinearScale(nominalMaterialAmount,
                    new Amount(component),
                    new Amount(model.Quantity, model.Unit));

                // BatchNumber, Amount
                var allocations = new List<Tuple<string, Amount>>();

                var alreadyAssigned = model.Materials.Where(m => (!string.IsNullOrWhiteSpace(m.BatchNumber)) && (m.MaterialId == component.MaterialId)).ToList();
                foreach (var alas in alreadyAssigned)
                {
                    model.Materials.Remove(alas);
                }

                if (component.Material.AutomaticBatches)
                {
                    var resolutions = m_batchFacade.AutoResolve(component.MaterialId, stepAmount, true).ToList();
                    allocations.AddRange(resolutions.Select(alo => new Tuple<string, Amount>(alo.Item1?.BatchNumber, alo.Item2)));
                }
                else
                {
                    var amountToAssign = stepAmount.Clone();
                    foreach (var alas in alreadyAssigned)
                    {
                        var assignedByThisBatch = m_amountProcessor.Min(amountToAssign, new Amount(alas.Amount, m_unitRepository.GetUnitBySymbol(alas.UnitSymbol)));

                        if (!string.IsNullOrWhiteSpace(alas.BatchNumber))
                        {
                            var batchObject =
                                batchIndex.FirstOrDefault(
                                    b => (b.MaterialId == alas.MaterialId) && (b.BatchNumber == alas.BatchNumber));
                            if (batchObject == null)
                            {
                                throw new InvalidOperationException("Fatal");
                            }
                            alas.BatchNumber = batchObject.BatchNumber;

                            assignedByThisBatch = m_amountProcessor.Min(assignedByThisBatch, m_batchFacade.GetAvailableAmount(batchObject.Id));
                            alas.Amount = assignedByThisBatch.Value;
                        }

                        allocations.Add(new Tuple<string, Amount>(alas.BatchNumber, assignedByThisBatch));

                        amountToAssign = m_amountProcessor.Subtract(amountToAssign, assignedByThisBatch);

                        if (!amountToAssign.IsPositive)
                        {
                            break;
                        }
                    }

                    if (amountToAssign.IsPositive)
                    {
                        allocations.Add(new Tuple<string, Amount>(null, amountToAssign));
                    }
                }

                foreach (var aloc in allocations)
                {
                    var resModel = new MaterialBatchResolutionModel
                    {
                        MaterialId = component.MaterialId,
                        MaterialName = component.Material.Name,
                        Amount = aloc.Item2.Value,
                        UnitSymbol = stepAmount.Unit.Symbol,
                        BatchNumber = aloc.Item1,
                        AutomaticBatches = component.Material.AutomaticBatches
                    };

                    model.Materials.Add(resModel);
                }
            }

            model.Materials.Sort(new GenericComparer<MaterialBatchResolutionModel>((a,b) => string.Compare(a.MaterialName, b.MaterialName, StringComparison.Ordinal)));
            model.IsValid = !model.Materials.Any(s => string.IsNullOrWhiteSpace(s.BatchNumber));
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
                                IsLocked = topBatch.LockDt != null
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

