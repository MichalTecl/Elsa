using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Production.Model;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Inventory;

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

        public ProductionFacade(
            IDatabase database,
            IMaterialBatchRepository batchRepository,
            IMaterialRepository materialRepository,
            IUnitConversionHelper unitConversion, 
            AmountProcessor amountProcessor, 
            ILog log, 
            IMaterialBatchFacade batchFacade)
        {
            m_database = database;
            m_batchRepository = batchRepository;
            m_materialRepository = materialRepository;
            m_unitConversion = unitConversion;
            m_amountProcessor = amountProcessor;
            m_log = log;
            m_batchFacade = batchFacade;
        }

        public ProductionBatchModel GetProductionBatch(int batchId)
        {
            return LoadAndValidateBatchModel(batchId);
        }

        public ProductionBatchModel CreateOrUpdateProductionBatch(
            int materialId,
            string batchNumber,
            decimal amount,
            IMaterialUnit unit)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var batchEntity =
                    m_batchRepository.GetMaterialBatches(
                        DateTime.Now.AddYears(-1),
                        DateTime.Now.AddYears(1),
                        false,
                        materialId,
                        true,
                        false,
                        true).FirstOrDefault()
                    ?? m_batchRepository.CreateProductionBatch(materialId, batchNumber, amount, unit);


                batchEntity.Batch.BatchNumber = batchNumber;

                if (batchEntity.Components.Any())
                {
                    if (Math.Abs(amount - batchEntity.ComponentAmount) > 0m || batchEntity.ComponentUnit.Id != unit.Id)
                    {
                        throw new InvalidOperationException(
                                  $"Nelze změnit množství či jednotku šarže, která má přiřazeny materiály");
                    }
                }
                else
                {
                    batchEntity.Batch.UnitId = unit.Id;
                    batchEntity.Batch.Volume = amount;
                }

                m_database.Save(batchEntity.Batch);

                var result = LoadAndValidateBatchModel(batchEntity.Batch.Id);

                tx.Commit();

                return result;
            }
        }

        public ProductionBatchModel AddComponentSourceBatch(
            int productionBatchId,
            int sourceBatchId,
            decimal usedAmount,
            string usedAmountUnitSymbol)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var sourceBatch = m_batchRepository.GetBatchById(sourceBatchId);
                if (sourceBatch == null)
                {
                    throw new InvalidOperationException("Požadovaná šarže nedostupná");
                }

                if (sourceBatch.IsClosed || sourceBatch.IsLocked || !sourceBatch.Batch.IsAvailable)
                {
                    throw new InvalidOperationException($"Šarže {sourceBatch.Batch.BatchNumber} je uzavřená, zamčená nebo není dokončená");
                }

                var batch = LoadAndValidateBatchModel(productionBatchId);
                if (batch == null)
                {
                    throw new InvalidOperationException("Šarže nedostupná");
                }

                var targetComponent = batch.Components.FirstOrDefault(c => c.MaterialId == sourceBatch.Batch.MaterialId);
                if (targetComponent == null)
                {
                    throw new InvalidOperationException($"Složení {batch.MaterialName} neobsahuje {sourceBatch.Batch.Material.Name}");
                }

                var batchPlaceholder = targetComponent.Assignments.SingleOrDefault(a => a.UsedBatchId == null);
                if (batchPlaceholder == null)
                {
                    throw new InvalidOperationException($"Potřebné množství materiálu \"{sourceBatch.Batch.Material.Name}\" již bylo přiřazeno");
                }

                var batchAvailableAmount = m_batchFacade.GetAvailableAmount(sourceBatchId);

                if (!batchAvailableAmount.IsPositive)
                {
                    throw new InvalidOperationException($"Šarže {sourceBatch.Batch.BatchNumber} je již plně spotřebována");
                }

                var amountToAllocate = m_amountProcessor.Min(
                    batchAvailableAmount,
                    new Amount(batchAvailableAmount.Value, batchAvailableAmount.Unit));

                m_batchFacade.AssignComponent(batch.BatchId, sourceBatch.Batch.Id, amountToAllocate);

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
                                MaterialName = topMaterial.Name
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
                                                UsedBatchId = subBatch.Component.Id
                                            };

                    componentModel.Assignments.Add(subBatchModel);
                }

                if (amountToResolve.IsNegative)
                {
                    complete = false;

                    m_log.Error($"PROBLEM - productionBatchId={productionBatchId}, topMaterial={topMaterial.Name}, requiredComponent.MaterialId={requiredComponent.Material.Name}, amountToResolve={amountToResolve}");
                    if (!subbatches.Any())
                    {
                        m_log.Error("FATAL - we have no batches assigned, but remaining amount is already < 0");
                        throw new InvalidOperationException("Došlo k fatální chybě, kontaktujte podporu");
                    }
                    return RemoveComponentSourceBatch(topBatch.Id, subbatches.Last().Id);
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
