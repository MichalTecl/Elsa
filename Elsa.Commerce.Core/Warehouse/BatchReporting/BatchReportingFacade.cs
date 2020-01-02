using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Model.BatchReporting;
using Elsa.Commerce.Core.Production;
using Elsa.Commerce.Core.SaleEvents;
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
        private readonly ISaleEventRepository m_saleEventRepository;

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
            IStockEventRepository stockEventRepository, 
            ISaleEventRepository saleEventRepository)
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
            m_saleEventRepository = saleEventRepository;
        }

        public BatchReportModel QueryBatches(BatchReportQuery query)
        {
            if (query.LoadOrdersPage != null)
            {
                return LoadOrders(query.ToKey(), query.LoadOrdersPage.Value);
            }
            else if (query.LoadSaleEventsPage != null)
            {
                return LoadSaleEvents(query.ToKey(), query.LoadSaleEventsPage.Value);
            }
            else if (query.LoadSegmentsPage != null)
            {
                return LoadSegments(query.ToKey(), query.LoadSegmentsPage.Value);
            }
            else if (query.LoadPriceComponentsPage != null)
            {
                return LoadPriceComponents(query.ToKey(), query.LoadPriceComponentsPage.Value);
            }

            var pageSize = query.HasKey ? 1 : c_pageSize;
            var pageNumber = query.HasKey ? 0 : query.PageNumber;

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
                .WithParam("@batchId", query.HasKey ? query.ToKey().UnsafeToString() : null) 
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
                .WithParam("@orderId", query.RelativeToOrderId)
                .WithParam("@onlyBlocking", query.BlockedBatchesOnly)
                .WithParam("@segmentId", query.SegmentId);

            var result = new BatchReportModel { Query = query };

            var rawEntries = sql.MapRows(MapEntry);
            
            result.Report.AddRange(rawEntries);
            result.CanLoadMore = (result.Report.Count == c_pageSize);

            foreach (var b in result.Report.OfType<BatchReportEntry>())
            {
                if (b.IsClosed)
                {
                    b.AvailableAmount = "0";
                }
                else
                {
                    var available = m_batchFacade.GetAvailableAmount(b.BatchKey);
                    b.AvailableAmount = $"{StringUtil.FormatDecimal(available.Value)} {available.Unit.Symbol}";
                    b.Available = available;
                }

                //b.NoDelReason = m_batchFacade.GetDeletionBlockReasons(b.BatchId).FirstOrDefault();
                b.CanDelete = !(b.HasStockEvents || b.NumberOfCompositions > 0 || b.NumberOfOrders > 0 || b.NumberOfSaleEvents > 0);

                if (b.HasStockEvents)
                {
                    PopulateStockEventCounts(b);
                }

                PopulateStockEventSuggestions(b);
            }

            if ((query.HasKey) && (result.Report.Count == 0))
            {
                result.Report.Add(new DeletedBatchReportEntry(query.ToKey()));
                return result;
            }

            if (query.CompositionId != null)
            {
                PopulateComponentAmounts(BatchKey.Parse(query.CompositionId), result.Report);
                result.CustomField1Name = "Použito";
            }
            else if (query.ComponentId != null)
            {
                PopulateCompositionAmounts(BatchKey.Parse(query.ComponentId), result.Report);
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
                var manipulationAmount = m_amountProcessor.ToSmallestUnit(amount);

                var sug = new BatchStockEventSuggestion()
                {
                    BatchNumber = batchReportEntry.BatchNumber,
                    Amount = manipulationAmount.Value,
                    EventTypeId = type.Id,
                    MaterialId = batchReportEntry.MaterialId,
                    MaterialName = batchReportEntry.MaterialName,
                    UnitSymbol = manipulationAmount.Unit.Symbol,
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
            foreach (var evt in m_stockEventRepository.GetBatchEvents(batchReportEntry.BatchKey))
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
                    var assignmentBatchKey = new BatchKey(assignment.MaterialBatchId);

                    var assignmentBatchMaterialId = assignmentBatchKey.GetMaterialId(m_batchFacade);
                    var assignmentBatchNumber = assignmentBatchKey.GetBatchNumber(m_batchFacade);

                    var sourceRecord = batches.OfType<BatchReportEntry>().FirstOrDefault(b => b.MaterialId == assignmentBatchMaterialId && b.BatchNumber.Equals(assignmentBatchNumber, StringComparison.InvariantCultureIgnoreCase));

                    var user = assignment == null ? string.Empty : m_userRepository.GetUserNick(assignment.UserId);

                    var reportRow = new BatchReportEntry(BatchKey.Parse(sourceRecord.BatchId))
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
                        NumberOfComponents = sourceRecord?.NumberOfComponents ?? 0,
                        NumberOfCompositions = sourceRecord?.NumberOfCompositions ?? 0,
                        NumberOfOrders = sourceRecord?.NumberOfOrders ?? 0,
                        Price = sourceRecord?.Price ?? string.Empty,
                        InvoiceNumber = sourceRecord?.InvoiceNumber ?? string.Empty,
                        NumberOfSaleEvents = sourceRecord?.NumberOfSaleEvents ?? 0,
                        NumberOfSegments = sourceRecord?.NumberOfSegments ?? 0
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

        private BatchReportModel LoadOrders(BatchKey key, int ordersPageNumber)
        {
            var orders = m_ordersFacade.GetOrdersByUsedBatch(key, c_pageSize, ordersPageNumber).ToList();

            var entry = new BatchOrdersReportEntry(key)
            {
                CanLoadMoreOrders = orders.Count == c_pageSize,
                NextOrdersPage = ordersPageNumber + 1
            };

            foreach (var entity in orders)
            {
                entry.Orders.Add(new BatchOrderModel
                {
                    OrderId = entity.Item1.Id,
                    Customer = entity.Item1.CustomerEmail,
                    OrderNumber = entity.Item1.OrderNumber,
                    PurchaseDate = StringUtil.FormatDateTime(entity.Item1.PurchaseDate),
                    Status = m_orderStatusRepository.Translate(entity.Item1.OrderStatusId),
                    Quantity = StringUtil.FormatDecimal(entity.Item2),
                    IsAllocation = !OrderStatus.IsSent(entity.Item1.OrderStatusId),
                    AllocationHandle = OrderStatus.IsSent(entity.Item1.OrderStatusId) ? null : $"{entity.Item1.Id}|{key.ToString(m_batchFacade)}"
                });
            }

            var result = new BatchReportModel();
            result.Report.Add(entry);

            return result;
        }

        private BatchReportModel LoadSaleEvents(BatchKey key, int pageNumber)
        {
            var events = m_saleEventRepository.GetAllocationsByBatch(key).ToList();

            var aggregatedAllocations = new List<SaleEventAllocationModel>(events.Count);

            foreach (var evt in events)
            {
                if (aggregatedAllocations.Any(ag => ag.Populate(evt, m_amountProcessor)))
                {
                    continue;
                }

                var newRecord = new SaleEventAllocationModel();
                newRecord.Populate(evt, m_amountProcessor);
                aggregatedAllocations.Add(newRecord);
            }

            var entry = new BatchSaleEventsReportEntry(key);
            entry.SaleEvents.AddRange(aggregatedAllocations.OrderByDescending(a => a.SortDt));

            var result = new BatchReportModel();
            result.Report.Add(entry);

            return result;
        }

        private BatchReportModel LoadSegments(BatchKey key, int pageNumber)
        {
            var segments = m_batchRepository.GetBatches(key).OrderBy(b => b.Created).ToList();

            var entry = new BatchSegmentsReportEntry(key);

            foreach (var b in segments)
            {
                entry.Segments.Add(new BatchSegmentModel
                {
                    SegmentId = b.Id,
                    Amount = new Amount(b.Volume, m_unitRepository.GetUnit(b.UnitId)).ToString(),
                    Author = m_userRepository.GetUserNick(b.AuthorId),
                    Date = StringUtil.FormatDate(b.Created),
                    Price = $"{StringUtil.FormatPrice(b.ProductionWorkPrice ?? 0m)} CZK",
                    HasRecipe = b.RecipeId != null
                });
            }

            var result = new BatchReportModel();
            result.Report.Add(entry);

            return result;
        }

        private BatchReportModel LoadPriceComponents(BatchKey key, int queryLoadPriceComponentsPage)
        {
            var entry = new PriceComponentsReportEntry(key);
            entry.PriceComponents.AddRange(m_batchFacade.GetPriceComponents(key));

            var result = new BatchReportModel();
            result.Report.Add(entry);

            return result;
        }

        private void PopulateCompositionAmounts(BatchKey componentBatchId, List<BatchReportEntryBase> report)
        {
            foreach (var reportRow in report.OfType<BatchReportEntry>())
            {
                var componentBatches = m_batchRepository.GetBatches(componentBatchId).ToList();
                if (!componentBatches.Any())
                {
                    continue;
                }

                Amount theAmount = null;

                foreach (var compnentBatch in componentBatches)
                {
                    foreach (var compositionBatch in m_batchRepository.GetBatches(reportRow.BatchKey))
                    {
                        foreach (var componentEntity in compositionBatch.Components)
                        {
                            var componentBatch = componentEntity.Component;
                            if (componentBatch.MaterialId != componentBatchId.GetMaterialId(m_batchRepository) ||
                                (!componentBatch.BatchNumber.Equals(componentBatchId.GetBatchNumber(m_batchRepository),
                                    StringComparison.InvariantCultureIgnoreCase)))
                            {
                                continue;
                            }

                            var usedAmount = new Amount(componentEntity.Volume, componentEntity.Unit);

                            theAmount = m_amountProcessor.Add(theAmount, usedAmount);
                        }
                    }
                }

                reportRow.CustomField1 = theAmount?.ToString();
            }
        }

        private void PopulateComponentAmounts(BatchKey compositionId, List<BatchReportEntryBase> resultReport)
        {
            var compositionPartials = m_batchRepository.GetBatches(compositionId);

            var usedAmounts = new Dictionary<string, Amount>(resultReport.Count);

            foreach (var composition in compositionPartials)
            {
                foreach (var componentEntity in composition.Components)
                {
                    var componentBatch = componentEntity.Component;
                    var componentBatchKey = new BatchKey(componentBatch.MaterialId, componentBatch.BatchNumber).UnsafeToString();

                    var usedAmount = new Amount(componentEntity.Volume, componentEntity.Unit);

                    if (usedAmounts.TryGetValue(componentBatchKey, out var rollingSum))
                    {
                        usedAmount = m_amountProcessor.Add(rollingSum, usedAmount);
                    }

                    usedAmounts[componentBatchKey] = usedAmount;
                }
            }
            
            foreach (var row in resultReport.OfType<BatchReportEntry>())
            {
                var usageKey = new BatchKey(row.MaterialId, row.BatchNumber).UnsafeToString();

                if (usedAmounts.TryGetValue(usageKey, out var amount))
                {
                    row.CustomField1 = amount.ToString();
                }
            }
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
            const int numberOfComponents = 13;
            const int numberOfCompositions = 14;
            const int numberOfOrders = 16;
            const int price = 17;
            const int invoiceNr = 18;
            const int numberOfStockEvents = 19;
            const int numberOfSaleEvents = 20;
            const int numberOfSegments = 21;
            #endregion

            var key = BatchKey.Parse(row.GetString(batchId));

            var entry = new BatchReportEntry(key)
            {
                InventoryName = row.GetString(inventoryName),
                BatchNumber = row.IsDBNull(batchNumber) ? "?" : row.GetString(batchNumber),
                MaterialName = row.GetString(materialName),
                MaterialId = row.GetInt32(materialId),
                BatchVolume = $"{StringUtil.FormatDecimal(row.GetDecimal(batchVolume))} {row.GetString(unit)}",
                CreateDt = StringUtil.FormatDateTime(row.GetDateTime(batchCreateDt)),
                IsClosed = !row.IsDBNull(batchCloseDt),
                IsLocked = !row.IsDBNull(batchLockDt),
                IsAvailable = row.GetBoolean(batchAvailable),
                NumberOfComponents = row.GetInt32(numberOfComponents),
                NumberOfCompositions = row.GetInt32(numberOfCompositions),
                NumberOfOrders = row.GetInt32(numberOfOrders),
                Price = row.IsDBNull(price) ? string.Empty : $"{StringUtil.FormatDecimal(row.GetDecimal(price))} CZK",
                InvoiceNumber = row.IsDBNull(invoiceNr) ? string.Empty : string.Join(", ", row.GetString(invoiceNr).Split(';').Distinct()),
                HasStockEvents = (!row.IsDBNull(numberOfStockEvents)) && (row.GetInt32(numberOfStockEvents) > 0),
                NumberOfSaleEvents = row.GetInt32(numberOfSaleEvents),
                NumberOfSegments = row.GetInt32(numberOfSegments)
            };
            
            return entry;
        }
    }
}
