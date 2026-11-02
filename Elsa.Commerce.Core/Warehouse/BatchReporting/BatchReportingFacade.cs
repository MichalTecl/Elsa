using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using Elsa.Apps.Inventory.Model;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Model.BatchReporting;
using Elsa.Commerce.Core.Production;
using Elsa.Commerce.Core.SaleEvents;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.BatchReporting
{
    public class BatchReportingFacade : IBatchReportingFacade
    {
        private const int c_pageSize = 10;

        private readonly ISession _session;
        private readonly IDatabase _database;
        private readonly IMaterialBatchFacade _batchFacade;
        private readonly IMaterialBatchRepository _batchRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IProductionFacade _productionFacade;
        private readonly AmountProcessor _amountProcessor;
        private readonly IUnitRepository _unitRepository;
        private readonly IOrdersFacade _ordersFacade;
        private readonly IOrderStatusRepository _orderStatusRepository;
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IStockEventRepository _stockEventRepository;
        private readonly ISaleEventRepository _saleEventRepository;
        private readonly ILog _log;

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
            ISaleEventRepository saleEventRepository,
            ILog log)
        {
            _session = session;
            _database = database;
            _batchFacade = batchFacade;
            _batchRepository = batchRepository;
            _materialRepository = materialRepository;
            _productionFacade = productionFacade;
            _amountProcessor = amountProcessor;
            _unitRepository = unitRepository;
            _ordersFacade = ordersFacade;
            _orderStatusRepository = orderStatusRepository;
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _stockEventRepository = stockEventRepository;
            _saleEventRepository = saleEventRepository;
            _log = log;
        }

        public BatchReportModel QueryBatches(BatchReportQuery query)
        {
            _log.Info($"Querying batches. Query={query}");

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

            if (   query.RelativeToOrderId != null 
                || query.ComponentId != null
                || query.CompositionId != null)
            {
                query.LoadBatchDetails = true;
                _log.Info("Loaing batch details for composition or component");
            }

            if (query.RelativeToOrderId != null)
            {
                order = _orderRepository.GetOrder(query.RelativeToOrderId.Value);

                if (order == null)
                {
                    throw new InvalidOperationException("Invalid entity reference");
                }

                pageSize = 1000;
                pageNumber = 0;
            }

            _log.Info("Calling LoadBatchesReport");

            var procedureName = query.LoadBatchDetails ? "LoadBatchesReport" : "LoadBatchesReport_Fast";

            var sqlStopwatch = _log.StartStopwatch($"Calling {procedureName} procedure");

            var sql = _database.Sql().Call(procedureName)
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@pageSize", pageSize)
                .WithParam("@pageNumber", pageNumber)
                .WithParam("@batchId", query.HasKey ? query.ToKey().UnsafeToString() : null) 
                .WithParam("@materialId", query.MaterialId)
                .WithParam("@orderNumber", ToProperNull(query.OrderNumber))
                .WithParam("@batchNumber",  ToProperNull(query.BatchNumberQuery?.Replace("*", "%")))
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
                .WithParam("@segmentId", query.SegmentId)
                .WithParam("@invoiceNr", ToProperNull(query.InvoiceNr));

            var result = new BatchReportModel { Query = query };

            var rawEntries = sql.MapRows(MapEntry);

            sqlStopwatch.Dispose();
            _log.Info($"loaded {rawEntries.Count} rows");

            result.Report.AddRange(rawEntries);
            result.CanLoadMore = (result.Report.Count == c_pageSize);

            _log.Info("Processing report entries");

            using (_log.StartStopwatch("Processing report entries"))
                foreach (var b in result.Report.OfType<BatchReportEntry>())
                {
                    b.HasDetail = query.LoadBatchDetails;

                    var material = _materialRepository.GetMaterialById(b.MaterialId);

                    if (b.IsClosed)
                    {
                        b.AvailableAmount = "0";
                    }
                    else
                    {
                        var available = new Amount(b.AvailableAmountValue, _unitRepository.GetUnit(b.AvailableAmountUnitId));

                        available = _amountProcessor.Convert(available, material.NominalUnit);
                        b.AvailableAmount = $"{StringUtil.FormatDecimal(available.Value)} {available.Unit.Symbol}";
                        b.Available = available;
                    }

                    var totalUnit = _unitRepository.GetUnitBySymbol(b.TotalAmountUnitName);
                    var totalAmount = new Amount(b.TotalAmountValue, totalUnit);
                    totalAmount = _amountProcessor.Convert(totalAmount, material.NominalUnit);

                    b.BatchVolume = $"{StringUtil.FormatDecimal(totalAmount.Value)} {totalAmount.Unit.Symbol}";

                    //b.NoDelReason = _batchFacade.GetDeletionBlockReasons(b.BatchId).FirstOrDefault();
                    b.CanDelete = !(b.HasStockEvents || b.NumberOfCompositions > 0 || b.NumberOfOrders > 0 || b.NumberOfSaleEvents > 0);

                    if (b.HasStockEvents)
                    {
                        PopulateStockEventCounts(b);
                    }

                    if (material.ExpirationMonths != null)
                    {
                        var expDt = b.CreatedRaw.AddMonths(material.ExpirationMonths.Value);
                        b.Expiration = StringUtil.FormatDateMonthYear(expDt);
                    }
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
        
        private void PopulateStockEventSuggestions(BatchMenuItems target, string batchNumber, IMaterial material, Amount available)
        {
            if (available.IsNotPositive)
            {
                return;
            }

            Func<IStockEventType, Amount, BatchStockEventSuggestion> addSuggestion = (type, amount) =>
            {
                var manipulationAmount = _amountProcessor.ToSmallestUnit(amount);

                var sug = new BatchStockEventSuggestion()
                {
                    BatchNumber = batchNumber,
                    Amount = manipulationAmount.Value,
                    EventTypeId = type.Id,
                    MaterialId = material.Id,
                    MaterialName = material.Name,
                    UnitSymbol = manipulationAmount.Unit.Symbol,
                    Title = $"{type.Name} {StringUtil.FormatDecimal(amount.Value)} {amount.Unit.Symbol}"
                };

                target.EventSuggestions.Add(sug);

                return sug;
            };

            foreach (var eventType in _stockEventRepository.GetAllEventTypes())
            {
                if (available.Unit.IntegerOnly)
                {
                    addSuggestion(eventType, new Amount(1m, available.Unit));
                }

                addSuggestion(eventType, available);
            }
        }

        private void PopulateProductionSuggestions(BatchMenuItems target, string batchNumber, IMaterial material, Amount available)
        {
            var prods = _batchFacade.GetOneClickProductionOptions().Where(p => p.BatchNumber == batchNumber && p.SourceMaterialId == material.Id).ToList();
            target.ProductionSuggestions.AddRange(prods);
        }

        private void PopulateStockEventCounts(BatchReportEntry batchReportEntry)
        {            
            foreach (var evt in _stockEventRepository.GetBatchEvents(batchReportEntry.BatchKey))
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
            var concreteItems = _ordersFacade.GetAllConcreteOrderItems(order).ToList();
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

                    var assignmentBatchMaterialId = assignmentBatchKey.GetMaterialId(_batchFacade);
                    var assignmentBatchNumber = assignmentBatchKey.GetBatchNumber(_batchFacade);

                    var sourceRecord = batches.OfType<BatchReportEntry>().FirstOrDefault(b => b.MaterialId == assignmentBatchMaterialId && b.BatchNumber.Equals(assignmentBatchNumber, StringComparison.InvariantCultureIgnoreCase));

                    var user = assignment == null ? string.Empty : _userRepository.GetUserNick(assignment.UserId);

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
            var orders = _ordersFacade.GetOrdersByUsedBatch(key, c_pageSize, ordersPageNumber).ToList();

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
                    Status = _orderStatusRepository.Translate(entity.Item1.OrderStatusId),
                    Quantity = StringUtil.FormatDecimal(entity.Item2),
                    IsAllocation = !OrderStatus.IsSent(entity.Item1.OrderStatusId),
                    AllocationHandle = OrderStatus.IsSent(entity.Item1.OrderStatusId) ? null : $"{entity.Item1.Id}|{key.ToString(_batchFacade)}"
                });
            }

            var result = new BatchReportModel();
            result.Report.Add(entry);

            return result;
        }

        private BatchReportModel LoadSaleEvents(BatchKey key, int pageNumber)
        {
            var events = _saleEventRepository.GetAllocationsByBatch(key).ToList();

            var aggregatedAllocations = new List<SaleEventAllocationModel>(events.Count);

            foreach (var evt in events)
            {
                if (aggregatedAllocations.Any(ag => ag.Populate(evt, _amountProcessor)))
                {
                    continue;
                }

                var newRecord = new SaleEventAllocationModel();
                newRecord.Populate(evt, _amountProcessor);
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
            var segments = _batchRepository.GetBatches(key).OrderBy(b => b.Created).ToList();

            var entry = new BatchSegmentsReportEntry(key);

            foreach (var b in segments)
            {
                entry.Segments.Add(new BatchSegmentModel
                {
                    SegmentId = b.Id,
                    Amount = new Amount(b.Volume, _unitRepository.GetUnit(b.UnitId)).ToString(),
                    Author = _userRepository.GetUserNick(b.AuthorId),
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
            entry.PriceComponents.AddRange(_batchFacade.GetPriceComponents(key));

            var result = new BatchReportModel();
            result.Report.Add(entry);

            return result;
        }

        private void PopulateCompositionAmounts(BatchKey componentBatchId, List<BatchReportEntryBase> report)
        {
            foreach (var reportRow in report.OfType<BatchReportEntry>())
            {
                var componentBatches = _batchRepository.GetBatches(componentBatchId).ToList();
                if (!componentBatches.Any())
                {
                    continue;
                }

                Amount theAmount = null;

                foreach (var compnentBatch in componentBatches)
                {
                    foreach (var compositionBatch in _batchRepository.GetBatches(reportRow.BatchKey))
                    {
                        foreach (var componentEntity in compositionBatch.Components)
                        {
                            var componentBatch = componentEntity.Component;
                            if (componentBatch.MaterialId != componentBatchId.GetMaterialId(_batchRepository) ||
                                (!componentBatch.BatchNumber.Equals(componentBatchId.GetBatchNumber(_batchRepository),
                                    StringComparison.InvariantCultureIgnoreCase)))
                            {
                                continue;
                            }

                            var usedAmount = new Amount(componentEntity.Volume, componentEntity.Unit);

                            theAmount = _amountProcessor.Add(theAmount, usedAmount);
                        }
                    }
                }

                reportRow.CustomField1 = theAmount?.ToString();
            }
        }

        private void PopulateComponentAmounts(BatchKey compositionId, List<BatchReportEntryBase> resultReport)
        {
            var compositionPartials = _batchRepository.GetBatches(compositionId);

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
                        usedAmount = _amountProcessor.Add(rollingSum, usedAmount);
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
            const int availableAmountValue = 22;
            const int availableAmountUnitId = 23;
            #endregion

            var key = BatchKey.Parse(row.GetString(batchId));

            var entry = new BatchReportEntry(key)
            {
                InventoryName = row.GetString(inventoryName),
                BatchNumber = row.IsDBNull(batchNumber) ? "?" : row.GetString(batchNumber),
                MaterialName = row.GetString(materialName),
                MaterialId = row.GetInt32(materialId),
                CreateDt = StringUtil.FormatDateTime(row.GetDateTime(batchCreateDt)),
                CreatedRaw = row.GetDateTime(batchCreateDt),
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
                NumberOfSegments = row.GetInt32(numberOfSegments),
                AvailableAmountValue = row.GetDecimal(availableAmountValue),
                AvailableAmountUnitId = row.GetInt32(availableAmountUnitId),
                TotalAmountValue = row.GetDecimal(batchVolume),
                TotalAmountUnitName = row.GetString(unit)
            };
            
            return entry;
        }

        private static object ToProperNull(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
                return null;

            return val;
        }

        public BatchMenuItems GetBatchMenuItems(BatchKey batchKey)
        {
            var result = new BatchMenuItems() { BatchKey = batchKey.UnsafeToString() };

            var available = _batchFacade.GetAvailableAmount(batchKey);

            var material = _materialRepository.GetMaterialById(batchKey.GetMaterialId(_batchFacade));

            PopulateStockEventSuggestions(result, batchKey.GetBatchNumber(_batchFacade), material.Adaptee, available);
            PopulateProductionSuggestions(result, batchKey.GetBatchNumber(_batchFacade), material.Adaptee, available);
            
            return result;
        }
    }
}
