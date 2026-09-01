using Elsa.App.OrdersInfo.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Core.Entities.Commerce.Integration;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.App.OrdersInfo.App
{
    public class OrderItemBatchAssignmentEditor
    {
        private const string ACCOUNTING_BLOCK_MESSAGE =
            "Objednávka již byla zahrnuta do účetnictví a nelze ji měnit.";

        private readonly IDatabase _db;
        private readonly ISession _session;
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly IVirtualProductFacade _virtualProductFacade;
        private readonly IMaterialBatchFacade _batchFacade;
        private readonly IMaterialBatchRepository _batchRepository;

        public OrderItemBatchAssignmentEditor(
            IDatabase db,
            ISession session,
            IPurchaseOrderRepository orderRepository,
            IVirtualProductFacade virtualProductFacade,
            IMaterialBatchFacade batchFacade,
            IMaterialBatchRepository batchRepository)
        {
            _db = db;
            _session = session;
            _orderRepository = orderRepository;
            _virtualProductFacade = virtualProductFacade;
            _batchFacade = batchFacade;
            _batchRepository = batchRepository;
        }

        public void Apply(OrderItemBatchAssignmentChangeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var changes = NormalizeChanges(request.Changes);
            if (changes.Count == 0)
                return;

            using (var tx = _db.OpenTransaction())
            {
                var realChangeDt = DateTime.Now;
                var order = _orderRepository.GetOrder(request.OrderId);
                if (order == null || order.ProjectId != _session.Project.Id)
                    throw new InvalidOperationException("Objednávka nebyla nalezena.");

                var item = GetAllOrderItems(order.Items)
                    .FirstOrDefault(orderItem => orderItem.Id == request.OrderItemId);
                if (item == null)
                    throw new InvalidOperationException("Položka objednávky nebyla nalezena.");

                var editBlockReason = GetEditBlockReason(order);
                if (editBlockReason != null)
                    throw new InvalidOperationException(editBlockReason);

                var assignments = _db.SelectFrom<IOrderItemMaterialBatch>()
                    .Join(assignment => assignment.MaterialBatch)
                    .Where(assignment => assignment.OrderItemId == item.Id)
                    .Execute()
                    .ToList();
                var originalAssignmentIds = new HashSet<long>(assignments.Select(assignment => assignment.Id));

                ValidateResultingQuantities(item, assignments, changes);

                foreach (var change in changes.Where(change => change.Delta < 0m))
                    RemoveAssignmentQuantity(assignments, change.BatchNumber, -change.Delta);

                foreach (var change in changes.Where(change => change.Delta > 0m))
                    AddAssignmentQuantity(order, item, change.BatchNumber, change.Delta);

                var effectiveChangeDt = GetEffectiveChangeDt(order, realChangeDt);
                SetNewAssignmentsDate(item.Id, originalAssignmentIds, effectiveChangeDt);

                var packingDtWasSet = false;
                if (!order.PackingDt.HasValue)
                {
                    order.PackingDt = effectiveChangeDt;
                    _db.Save(order);
                    packingDtWasSet = true;
                }

                WriteProcessingLog(
                    order,
                    item,
                    changes,
                    realChangeDt,
                    effectiveChangeDt,
                    packingDtWasSet);

                tx.Commit();
            }
        }

        private static List<OrderItemBatchAssignmentDeltaModel> NormalizeChanges(
            IEnumerable<OrderItemBatchAssignmentDeltaModel> source)
        {
            var changes = (source ?? Enumerable.Empty<OrderItemBatchAssignmentDeltaModel>())
                .Where(change => change != null && change.Delta != 0m)
                .ToList();

            if (changes.Any(change => string.IsNullOrWhiteSpace(change.BatchNumber)))
                throw new InvalidOperationException("Číslo šarže musí být vyplněno.");

            return changes
                .GroupBy(change => change.BatchNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new OrderItemBatchAssignmentDeltaModel
                {
                    BatchNumber = group.Key,
                    Delta = group.Sum(change => change.Delta)
                })
                .Where(change => change.Delta != 0m)
                .ToList();
        }

        private static void ValidateResultingQuantities(
            IOrderItem item,
            IReadOnlyCollection<IOrderItemMaterialBatch> assignments,
            IReadOnlyCollection<OrderItemBatchAssignmentDeltaModel> changes)
        {
            var resultingTotal = assignments.Sum(assignment => assignment.Quantity)
                                 + changes.Sum(change => change.Delta);
            if (resultingTotal < 0m)
                throw new InvalidOperationException("Výsledné přiřazené množství nesmí být záporné.");

            if (resultingTotal > item.Quantity)
            {
                throw new InvalidOperationException(
                    $"Celkové přiřazené množství {resultingTotal} překračuje množství položky {item.Quantity}.");
            }

            foreach (var change in changes)
            {
                var currentBatchQuantity = assignments
                    .Where(assignment => string.Equals(
                        assignment.MaterialBatch?.BatchNumber,
                        change.BatchNumber,
                        StringComparison.OrdinalIgnoreCase))
                    .Sum(assignment => assignment.Quantity);

                if (currentBatchQuantity + change.Delta < 0m)
                {
                    throw new InvalidOperationException(
                        $"Ze šarže {change.BatchNumber} nelze odebrat větší množství, než je přiřazeno.");
                }
            }
        }

        private void RemoveAssignmentQuantity(
            IReadOnlyCollection<IOrderItemMaterialBatch> assignments,
            string batchNumber,
            decimal quantityToRemove)
        {
            var matchingAssignments = assignments
                .Where(assignment => string.Equals(
                    assignment.MaterialBatch?.BatchNumber,
                    batchNumber,
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(assignment => assignment.AssignmentDt)
                .ToList();

            var remaining = quantityToRemove;
            foreach (var assignment in matchingAssignments)
            {
                if (remaining <= 0m)
                    break;

                var removed = Math.Min(assignment.Quantity, remaining);
                assignment.Quantity -= removed;
                remaining -= removed;

                if (assignment.Quantity == 0m)
                    _db.Delete(assignment);
                else
                    _db.Save(assignment);

                _batchFacade.ReleaseBatchAmountCache(assignment.MaterialBatchId);
            }

            if (remaining > 0m)
            {
                throw new InvalidOperationException(
                    $"Ze šarže {batchNumber} nelze odebrat větší množství, než je přiřazeno.");
            }
        }

        private void AddAssignmentQuantity(
            IPurchaseOrder order,
            IOrderItem item,
            string batchNumber,
            decimal quantityToAdd)
        {
            var material = _virtualProductFacade.GetOrderItemMaterialForSingleUnit(order, item);
            var batchKey = _batchFacade.FindBatchBySearchQuery(material.MaterialId, batchNumber);
            var batches = _batchRepository.GetBatches(batchKey).OrderBy(batch => batch.Created).ToList();
            var remaining = quantityToAdd;

            foreach (var batch in batches)
            {
                if (remaining <= 0m)
                    break;

                var available = _batchFacade.GetAvailableAmount(batch.Id).Value;
                if (available <= 0m)
                    continue;

                var allocated = Math.Min(remaining, available);
                _batchFacade.AssignOrderItemToBatch(
                    batch.Id,
                    order,
                    item.Id,
                    allocated,
                    out _);
                remaining -= allocated;
            }

            if (remaining > 0m)
            {
                throw new InvalidOperationException(
                    $"V šarži {batchNumber} není dostatečné množství. Chybí {remaining}.");
            }
        }

        private void SetNewAssignmentsDate(
            long orderItemId,
            ISet<long> originalAssignmentIds,
            DateTime assignmentDt)
        {
            var newAssignments = _db.SelectFrom<IOrderItemMaterialBatch>()
                .Where(assignment => assignment.OrderItemId == orderItemId)
                .Execute()
                .Where(assignment => !originalAssignmentIds.Contains(assignment.Id));

            foreach (var assignment in newAssignments)
            {
                assignment.AssignmentDt = assignmentDt;
                _db.Save(assignment);
            }
        }

        public string GetBatchAssignmentDateNotice(IPurchaseOrder order, DateTime now)
        {
            if (!UsesHistoricalChangeDate(order, now))
                return null;

            return
                $"Objednávka patří do období {order.BuyDate.Month:00}/{order.BuyDate.Year}. " +
                $"Nová přiřazení šarží a případné doplnění data zabalení se uloží s datem " +
                $"{order.BuyDate:d. M. yyyy}, aby změna zůstala ve správném účetním období.";
        }

        private static DateTime GetEffectiveChangeDt(IPurchaseOrder order, DateTime now)
        {
            return UsesHistoricalChangeDate(order, now) ? order.BuyDate : now;
        }

        private static bool UsesHistoricalChangeDate(IPurchaseOrder order, DateTime now)
        {
            return order.BuyDate != default(DateTime)
                   && (order.BuyDate.Year != now.Year || order.BuyDate.Month != now.Month);
        }

        public string GetEditBlockReason(IPurchaseOrder order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var itemIds = GetAllOrderItems(order.Items).Select(item => item.Id).Distinct().ToList();
            if (itemIds.Count > 0)
            {
                var assignmentIds = _db.SelectFrom<IOrderItemMaterialBatch>()
                    .Where(assignment => assignment.OrderItemId.InCsv(itemIds))
                    .Execute()
                    .Select(assignment => assignment.Id)
                    .ToList();

                if (assignmentIds.Count > 0
                    && _db.SelectFrom<IOrderItemInvoiceFormItem>()
                        .Where(bridge => bridge.BatchAssignmentId.InCsv(assignmentIds))
                        .Execute()
                        .Any())
                {
                    return ACCOUNTING_BLOCK_MESSAGE;
                }
            }

            if (order.BuyDate == default(DateTime))
                return null;

            var buyDate = order.BuyDate;
            var closureExists = false;
            _db.Sql()
                .ExecuteWithParams(
                    "SELECT TOP 1 1 FROM FinDataGenerationClosure " +
                    "WHERE ProjectId = {0} AND [Year] = {1} AND [Month] = {2}",
                    order.ProjectId,
                    buyDate.Year,
                    buyDate.Month)
                .ReadRows<int>(_ => closureExists = true);

            return closureExists
                ? $"Objednávka spadá do již vyúčtovaného období {buyDate.Month:00}/{buyDate.Year} a nelze ji měnit."
                : null;
        }

        private void WriteProcessingLog(
            IPurchaseOrder order,
            IOrderItem item,
            IReadOnlyCollection<OrderItemBatchAssignmentDeltaModel> changes,
            DateTime realChangeDt,
            DateTime effectiveChangeDt,
            bool packingDtWasSet)
        {
            var changeText = string.Join(", ", changes.Select(change =>
                $"{(change.Delta > 0m ? "přidáno" : "odebráno")} {Math.Abs(change.Delta)}× šarže {change.BatchNumber}"));
            var message =
                $"Ruční změna přiřazení šarží uživatelem {_session.User.EMail}: " +
                $"objednávka {order.OrderNumber}, položka \"{item.PlacedName}\" (ID {item.Id}); {changeText}. " +
                $"Datum skladové změny {effectiveChangeDt:d. M. yyyy H:mm}." +
                (packingDtWasSet ? " Stejné datum bylo doplněno do PackingDt objednávky." : string.Empty);

            if (message.Length > 1000)
                message = message.Substring(0, 997) + "...";

            var log = _db.New<IOrderProcessingLog>();
            log.PurchaseOrderId = order.Id;
            log.ProcessDt = realChangeDt;
            log.ProcessCode = message;
            _db.Save(log);
        }

        private static IEnumerable<IOrderItem> GetAllOrderItems(IEnumerable<IOrderItem> items)
        {
            foreach (var item in items)
            {
                yield return item;

                foreach (var child in GetAllOrderItems(item.KitChildren))
                    yield return child;
            }
        }
    }
}
