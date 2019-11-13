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

