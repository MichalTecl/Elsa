using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Http;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Model.BatchReporting;
using Elsa.Commerce.Core.Production;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

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
        private readonly IOrdersFacade m_ordersFacade;
        private readonly IOrderStatusRepository m_orderStatusRepository;
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IUserRepository m_userRepository;
        private readonly IStockEventRepository m_stockEventRepository;

        public BatchReportingFacade(ISession session,
            IDatabase database,
            IMaterialBatchFacade batchFacade,
            IMaterialBatchRepository batchRepository,
            IMaterialRepository materialRepository,
            IProductionFacade productionFacade,
            AmountProcessor amountProcessor,
            IUnitRepository unitRepository,
            IOrdersFacade ordersFacade,
            IOrderStatusRepository orderStatusRepository,
            IPurchaseOrderRepository orderRepository,
            IUserRepository userRepository,
            IStockEventRepository stockEventRepository)
        {
            m_session = session;
            m_database = database;
            m_batchFacade = batchFacade;
            m_batchRepository = batchRepository;
            m_materialRepository = materialRepository;
            m_productionFacade = productionFacade;
            m_amountProcessor = amountProcessor;
            m_unitRepository = unitRepository;
            m_ordersFacade = ordersFacade;
            m_orderStatusRepository = orderStatusRepository;
            m_orderRepository = orderRepository;
            m_userRepository = userRepository;
            m_stockEventRepository = stockEventRepository;
        }

        public BatchReportModel QueryBatches(BatchReportQuery query)
        {
            if (query.LoadSteps)
            {
                return LoadSteps(query.BatchId ?? -1);
            }
            else if (query.LoadOrdersPage != null)
            {
                return LoadOrders(query.BatchId ?? -1, query.LoadOrdersPage.Value);
            }
            
            var pageSize = query.BatchId == null ? c_pageSize : 1;
            var pageNumber = query.BatchId == null ? query.PageNumber : 0;

            IPurchaseOrder order = null;

            if (query.RelativeToOrderId != null)
            {
                order = m_orderRepository.GetOrder(query.RelativeToOrderId.Value);

                if (order == null)
                {
                    throw new InvalidOperationException("Invalid entity reference");
                }

                pageSize = 1000;
                pageNumber = 0;
            }

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
                .WithParam("@componentId", query.ComponentId)
                .WithParam("@orderId", query.RelativeToOrderId);

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
                    b.Available = available;
                }

                b.NoDelReason = m_batchFacade.GetDeletionBlockReasons(b.BatchId).FirstOrDefault();
                b.CanDelete = string.IsNullOrWhiteSpace(b.NoDelReason);

                if (b.HasStockEvents)
                {
                    PopulateStockEventCounts(b);
                }

                PopulateStockEventSuggestions(b);
            }

            if ((query.BatchId != null) && (result.Report.Count == 0))
            {
                result.Report.Add(new DeletedBatchReportEntry(query.BatchId.Value));
                return result;
            }

            if (query.CompositionId != null)
            {
                PopulateComponentAmounts(query.CompositionId.Value, result.Report);
                result.CustomField1Name = "Použito";
            }
            else if (query.ComponentId != null)
            {
                PopulateCompositionAmounts(query.ComponentId.Value, result.Report);
                result.CustomField1Name = "Použito";
            }
            else if (query.RelativeToOrderId != null)
            {
                result.Report = PopulateRelativeToOrder(order, result.Report);
                result.CustomField1Name = "Množství";
                result.CustomField3Name = "Položka";
                result.CustomField2Name = "Balil";

            }
            
            return result;
        }

        private void PopulateStockEventSuggestions(BatchReportEntry batchReportEntry)
        {
            if (batchReportEntry.Available?.IsNotPositive ?? true)
            {
                return;
            }

            Func<IStockEventType, Amount, BatchStockEventSuggestion> addSuggestion = (type, amount) =>
            {
                var sug = new BatchStockEventSuggestion()
                {
                    BatchNumber = batchReportEntry.BatchNumber,
                    Amount = amount.Value,
                    EventTypeId = type.Id,
                    MaterialId = batchReportEntry.MaterialId,
                    MaterialName = batchReportEntry.MaterialName,
                    UnitId = amount.Unit.Id,
                    Title = $"{type.Name} {StringUtil.FormatDecimal(amount.Value)} {amount.Unit.Symbol}"
                };

                batchReportEntry.EventSuggestions.Add(sug);

                return sug;
            };

            foreach (var eventType in m_stockEventRepository.GetAllEventTypes())
            {
                if (batchReportEntry.Available.Unit.IntegerOnly)
                {
                    addSuggestion(eventType, new Amount(1m, batchReportEntry.Available.Unit));
                }

                addSuggestion(eventType, batchReportEntry.Available);
            }
        }

        private void PopulateStockEventCounts(BatchReportEntry batchReportEntry)
        {
            foreach (var evt in m_stockEventRepository.GetBatchEvents(batchReportEntry.BatchId))
            {
                int sum;
                if (!batchReportEntry.StockEventCounts.TryGetValue(evt.Type.TabTitle, out sum))
                {
                    sum = 0;
                }

                batchReportEntry.StockEventCounts[evt.Type.TabTitle] = sum + 1;
            }
        }

        private List<BatchReportEntryBase> PopulateRelativeToOrder(IPurchaseOrder order, List<BatchReportEntryBase> batches)
        {
            var concreteItems = m_ordersFacade.GetAllConcreteOrderItems(order).ToList();
            var result = new List<BatchReportEntryBase>(concreteItems.Count * 2);

            foreach (var orderItem in concreteItems)
            {
                var itemName = orderItem.KitParent?.PlacedName ?? orderItem.PlacedName;

                var assignedBatches = orderItem.AssignedBatches.ToList();
                if (!assignedBatches.Any())
                {
                    assignedBatches.Add(null);
                }

                foreach (var assignment in assignedBatches)
                {
                    var sourceRecord = batches.OfType<BatchReportEntry>().FirstOrDefault(b => b.BatchId == assignment?.MaterialBatchId);

                    var user = assignment == null ? string.Empty : m_userRepository.GetUserNick(assignment.UserId);

                    var reportRow = new BatchReportEntry(assignment?.MaterialBatchId ?? -1)
                    {
                        CustomField1 = StringUtil.FormatDecimal(assignment?.Quantity ?? orderItem.Quantity),
                        CustomField2 = user,
                        CustomField3 = itemName,
                        InventoryName = sourceRecord?.InventoryName,
                        BatchNumber = sourceRecord?.BatchNumber ?? string.Empty,
                        MaterialName = sourceRecord?.MaterialName ?? string.Empty,
                        MaterialId = sourceRecord?.MaterialId ?? -1,
                        BatchVolume = sourceRecord?.BatchVolume ?? string.Empty,
                        AvailableAmount = sourceRecord?.AvailableAmount ?? string.Empty,
                        CreateDt = sourceRecord?.CreateDt ?? string.Empty,
                        IsClosed = sourceRecord?.IsClosed ?? false,
                        IsLocked = sourceRecord?.IsLocked ?? false,
                        IsAvailable = sourceRecord?.IsAvailable ?? false,
                        AllStepsDone = sourceRecord?.AllStepsDone ?? false,
                        NumberOfComponents = sourceRecord?.NumberOfComponents ?? 0,
                        NumberOfCompositions = sourceRecord?.NumberOfCompositions ?? 0,
                        NumberOfRequiredSteps = sourceRecord?.NumberOfRequiredSteps ?? 0,
                        NumberOfOrders = sourceRecord?.NumberOfOrders ?? 0,
                        Price = sourceRecord?.Price ?? string.Empty,
                        InvoiceNumber = sourceRecord?.InvoiceNumber ?? string.Empty
                    };

                    result.Add(reportRow);
                }
            }

            result.Sort(
                new GenericComparer<BatchReportEntryBase>(
                    (a, b) =>
                        string.Compare((a as BatchReportEntry)?.CustomField2,
                            (b as BatchReportEntry)?.CustomField2,
                            StringComparison.Ordinal)));

            return result;
        }

        private BatchReportModel LoadOrders(int queryBatchId, int ordersPageNumber)
        {
            var orders = m_ordersFacade.GetOrdersByUsedBatch(queryBatchId, c_pageSize, ordersPageNumber).ToList();

            var entry = new BatchOrdersReportEntry(queryBatchId)
            {
                CanLoadMoreOrders = orders.Count == c_pageSize,
                NextOrdersPage = ordersPageNumber + 1
            };

            foreach (var entity in orders)
            {
                entry.Orders.Add(new BatchOrderModel
                {
                    OrderId = entity.Id,
                    Customer = entity.CustomerEmail,
                    OrderNumber = entity.OrderNumber,
                    PurchaseDate = StringUtil.FormatDateTime(entity.PurchaseDate),
                    Status = m_orderStatusRepository.Translate(entity.OrderStatusId)
                });
            }

            var result = new BatchReportModel();
            result.Report.Add(entry);

            return result;
        }

        private void PopulateCompositionAmounts(int componentBatchId, List<BatchReportEntryBase> report)
        {
            var thisBatch = m_batchRepository.GetBatchById(componentBatchId);
            if (thisBatch == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }
            
            var zeroAmount = new Amount(0, thisBatch.ComponentUnit);

            foreach (var row in report.OfType<BatchReportEntry>())
            {
                var amount = zeroAmount.Clone();

                var componentBatch = m_batchRepository.GetBatchById(row.BatchId);
                foreach (var component in componentBatch.Components.Where(c => c.Batch.Id == componentBatchId))
                {
                    amount = m_amountProcessor.Add(amount,
                        new Amount(component.ComponentAmount, component.ComponentUnit));
                }

                if (row.NumberOfRequiredSteps > 0)
                {
                    foreach (var stp in componentBatch.Batch.PerformedSteps.SelectMany(s => s.SourceBatches).Where(s => s.SourceBatchId == componentBatchId))
                    {
                        amount = m_amountProcessor.Add(amount, m_amountProcessor.ToAmount(stp.UsedAmount, stp.UnitId));
                    }
                }

                row.CustomField1 = amount.ToString();
            }
        }

        private void PopulateComponentAmounts(int compositionId, List<BatchReportEntryBase> resultReport)
        {
            var composition = m_batchRepository.GetBatchById(compositionId);
            
            foreach (var row in resultReport.OfType<BatchReportEntry>())
            {
                var componentBatchId = row.BatchId;
                var amount = new Amount(0, m_materialRepository.GetMaterialById(row.MaterialId).NominalUnit);
                
                var components = composition.Components.Where(c => componentBatchId == c.Batch.Id);

                foreach (var component in components)
                {
                    amount = m_amountProcessor.Add(amount, new Amount(component.ComponentAmount, component.ComponentUnit));
                }

                foreach (var steps in composition.Batch.PerformedSteps)
                {
                    foreach (var stepComponent in steps.SourceBatches.Where(sb => sb.SourceBatchId == componentBatchId))
                    {
                        amount = m_amountProcessor.Add(amount,
                            new Amount(stepComponent.UsedAmount, m_unitRepository.GetUnit(stepComponent.UnitId)));
                    }
                }

                row.CustomField1 = amount.ToString();
            }
        }

        private BatchReportModel LoadSteps(int queryBatchId)
        {
            var batch = m_batchRepository.GetBatchById(queryBatchId);
            if (batch == null)
            {
                throw new InvalidOperationException("not found");
            }

            var batchStatus = m_batchFacade.GetBatchStatus(queryBatchId);

            var requiredSteps = m_materialRepository.GetMaterialProductionSteps(batch.Batch.MaterialId).Ordered().ToList();

            if (!requiredSteps.Any())
            {
                throw new InvalidOperationException("invalid request");
            }

            var entry = new BatchProductionStepReportEntry(queryBatchId);

            var performedSteps = batch.Batch.PerformedSteps.ToList();
            foreach (var requiredStep in requiredSteps)
            {
                var stepModel = new BatchProductionStepReportEntry.ProductionStepModel
                {
                    MaterialStepId = requiredStep.Id,
                    StepName = requiredStep.Name,
                    BatchId = batch.Batch.Id
                };

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
                        CanDelete =  m_productionFacade.CheckProductionStepCanBeDeleted(batchStatus, doneStep.Id, batch.Batch)
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
            const int price = 17;
            const int invoiceNr = 18;
            const int numberOfStockEvents = 19;
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
                NumberOfOrders = row.GetInt32(numberOfOrders),
                Price = row.IsDBNull(price) ? string.Empty : $"{StringUtil.FormatDecimal(row.GetDecimal(price))} CZK",
                InvoiceNumber = row.IsDBNull(invoiceNr) ? string.Empty : row.GetString(invoiceNr),
                HasStockEvents = (!row.IsDBNull(numberOfStockEvents)) && (row.GetInt32(numberOfStockEvents) > 0)
            };
            
            return entry;
        }
    }
}
