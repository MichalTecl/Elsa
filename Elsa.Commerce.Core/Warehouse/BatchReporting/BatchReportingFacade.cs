using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using Elsa.Commerce.Core.Model.BatchReporting;
using Elsa.Commerce.Core.Production;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Extensions;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.BatchReporting
{
    public class BatchReportingFacade : IBatchReportingFacade
    {
        private const int c_pageSize = 10;

        private readonly ISession m_session;
        private readonly IDatabase m_database;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IProductionFacade m_productionFacade;
        private readonly AmountProcessor m_amountProcessor;
        private readonly IUnitRepository m_unitRepository;

        public BatchReportingFacade(ISession session,
            IDatabase database,
            IMaterialBatchFacade batchFacade,
            IMaterialBatchRepository batchRepository,
            IMaterialRepository materialRepository,
            IProductionFacade productionFacade,
            AmountProcessor amountProcessor,
            IUnitRepository unitRepository)
        {
            m_session = session;
            m_database = database;
            m_batchFacade = batchFacade;
            m_batchRepository = batchRepository;
            m_materialRepository = materialRepository;
            m_productionFacade = productionFacade;
            m_amountProcessor = amountProcessor;
            m_unitRepository = unitRepository;
        }

        public BatchReportModel QueryBatches(BatchReportQuery query)
        {
            if (query.LoadSteps)
            {
                return LoadSteps(query.BatchId ?? -1);
            }




            var pageSize = query.BatchId == null ? c_pageSize : 1;
            var pageNumber = query.BatchId == null ? query.PageNumber : 0;

            var sql = m_database.Sql().Call("LoadBatchesReport")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@pageSize", pageSize)
                .WithParam("@pageNumber", pageNumber)
                .WithParam("@batchId", query.BatchId)
                .WithParam("@materialId", query.MaterialId)
                .WithParam("@orderNumber", query.OrderNumber)
                .WithParam("@batchNumber", query.BatchNumberQuery?.Replace("*", "%"))
                .WithParam("@dtFrom", query.From)
                .WithParam("@dtTo", query.To)
                .WithParam("@closed", query.ClosedBatches)
                .WithParam("@locked", query.LockedBatches)
                .WithParam("@inventoryTypeId", query.InventoryTypeId)
                .WithParam("@onlyProduced", query.ProducedOnly)
                .WithParam("@onlyBought", query.PurchasedOnly)
                .WithParam("@compositionId", query.CompositionId)
                .WithParam("@componentId", query.ComponentId);

            var result = new BatchReportModel { Query = query };
            result.Report.AddRange(sql.MapRows(MapEntry));
            result.CanLoadMore = (result.Report.Count == c_pageSize);

            foreach (var b in result.Report.OfType<BatchReportEntry>())
            {
                if (b.IsClosed)
                {
                    b.AvailableAmount = "0";
                }
                else
                {
                    var available = m_batchFacade.GetAvailableAmount(b.BatchId);
                    b.AvailableAmount = $"{StringUtil.FormatDecimal(available.Value)} {available.Unit.Symbol}";
                }
            }

            return result;
        }

        private BatchReportModel LoadSteps(int queryBatchId)
        {
            var batch = m_batchRepository.GetBatchById(queryBatchId);
            if (batch == null)
            {
                throw new InvalidOperationException("not found");
            }

            var requiredSteps = m_materialRepository.GetMaterialProductionSteps(batch.Batch.MaterialId).Ordered().ToList();

            if (!requiredSteps.Any())
            {
                throw new InvalidOperationException("invalid request");
            }

            var entry = new BatchProductionStepReportEntry(queryBatchId);

            var performedSteps = batch.Batch.PerformedSteps.ToList();
            foreach (var requiredStep in requiredSteps)
            {
                var stepModel = new BatchProductionStepReportEntry.ProductionStepModel();
                stepModel.MaterialStepId = requiredStep.Id;
                stepModel.StepName = requiredStep.Name;

                entry.Steps.Add(stepModel);

                var done =
                    performedSteps.Where(s => (s.BatchId == queryBatchId) && (s.StepId == requiredStep.Id)).ToList();

                var producedAmount = new Amount(0, batch.ComponentUnit);
                foreach (var doneStep in done)
                {
                    var doneStepAmount = new Amount(doneStep.ProducedAmount, batch.ComponentUnit);
                    producedAmount = m_amountProcessor.Add(producedAmount, doneStepAmount);

                    var doneModel = new BatchProductionStepReportEntry.PerformedStepModel()
                    {
                        Amount = doneStepAmount.ToString(),
                        ConfirmDt = StringUtil.FormatDateTime(doneStep.ConfirmDt),
                        ConfirmUser = doneStep.ConfirmUser.EMail,
                        Price = StringUtil.FormatDecimal(doneStep.Price ?? 0m),
                        SpentHours = StringUtil.FormatDecimal(doneStep.SpentHours ?? 0m),
                        StepId = doneStep.Id,
                        Worker = doneStep.Worker?.EMail,
                    };

                    stepModel.PerformedSteps.Add(doneModel);

                    foreach (var component in doneStep.SourceBatches)
                    {
                        var sourceBatch = m_batchRepository.GetBatchById(component.SourceBatchId);
                        var material = m_materialRepository.GetMaterialById(sourceBatch.Batch.MaterialId);
                        var componentModel = new BatchProductionStepReportEntry.PerfomedStepComponent()
                        {
                            MaterialName = material.Name,
                            Amount = new Amount(component.UsedAmount, m_unitRepository.GetUnit(component.UnitId)).ToString(),
                            BatchNumber = m_batchRepository.GetBatchNumberById(component.SourceBatchId),
                            StepBatchId = component.Id
                        };

                        doneModel.Components.Add(componentModel);
                    }

                    doneModel.Components.Sort(
                        new GenericComparer<BatchProductionStepReportEntry.PerfomedStepComponent>(
                            (a, b) => string.Compare(a.MaterialName, b.MaterialName, StringComparison.Ordinal)));
                }

                var perc = producedAmount.IsNotPositive
                    ? decimal.Zero
                    : m_amountProcessor.Divide(producedAmount, new Amount(batch.ComponentAmount, batch.ComponentUnit))
                        .Value;

                perc = perc*100m;

                stepModel.DonePercent = $"{StringUtil.FormatDecimal(Math.Round(perc, 0))}%";
            }
            
            var report = new BatchReportModel()
            {
                CanLoadMore = false,
                IsUpdate = true
            };

            report.Report.Add(entry);

            return report;
        }

        private BatchReportEntry MapEntry(DbDataReader row)
        {
            #region Column ordinals
            const int batchId = 0;
            const int inventoryName = 1;
            const int batchNumber = 2;
            const int materialName = 3;
            const int materialId = 4;
            const int batchVolume = 5;
            const int unit = 6;
            const int batchCreateDt = 7;
            const int batchCloseDt = 8;
            const int batchLockDt = 9;
            const int batchAvailable = 10;
            const int batchProductionDt = 11;
            const int batchStepsDone = 12;
            const int numberOfComponents = 13;
            const int numberOfCompositions = 14;
            const int numberOfRequiredSteps = 15;
            const int numberOfOrders = 16;
            #endregion


            var entry = new BatchReportEntry(row.GetInt32(batchId))
            {
                InventoryName = row.GetString(inventoryName),
                BatchNumber = row.GetString(batchNumber),
                MaterialName = row.GetString(materialName),
                MaterialId = row.GetInt32(materialId),
                BatchVolume = $"{StringUtil.FormatDecimal(row.GetDecimal(batchVolume))} {row.GetString(unit)}",
                CreateDt = StringUtil.FormatDateTime(row.GetDateTime(batchCreateDt)),
                IsClosed = !row.IsDBNull(batchCloseDt),
                IsLocked = !row.IsDBNull(batchLockDt),
                IsAvailable = row.GetBoolean(batchAvailable),
                AllStepsDone = (!row.IsDBNull(batchStepsDone)) && row.GetBoolean(batchStepsDone),
                NumberOfComponents = row.GetInt32(numberOfComponents),
                NumberOfCompositions = row.GetInt32(numberOfCompositions),
                NumberOfRequiredSteps = row.GetInt32(numberOfRequiredSteps),
                NumberOfOrders = row.GetInt32(numberOfOrders)
            };
            
            return entry;
        }
    }
}
