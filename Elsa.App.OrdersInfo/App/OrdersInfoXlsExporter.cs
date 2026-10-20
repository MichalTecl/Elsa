using Elsa.App.OrdersInfo.Model;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using OfficeOpenXml;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using XlsSerializer.Core;

namespace Elsa.App.OrdersInfo.App
{
    public class OrdersInfoXlsExporter
    {
        private static readonly CultureInfo _czechCulture = CultureInfo.GetCultureInfo("cs-CZ");

        private readonly IDatabase _db;
        private readonly ISession _session;

        public OrdersInfoXlsExporter(IDatabase db, ISession session)
        {
            _db = db;
            _session = session;
        }

        public byte[] Export(Action<IQueryBuilder<IPurchaseOrder>> filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            var query = _db.SelectFrom<IPurchaseOrder>()
                .Join(order => order.Erp)
                .Join(order => order.Currency)
                .Join(order => order.OrderStatus)
                .Join(order => order.InvoiceAddress)
                .Join(order => order.DeliveryAddress)
                .Join(order => order.InsertUser)
                .Join(order => order.PaymentPairingUser)
                .Join(order => order.PackingUser)
                .Join(order => order.Payment)
                .Join(order => order.Payment.PaymentSource)
                .Join(order => order.Payment.Currency)
                .Where(order => order.ProjectId == _session.Project.Id)
                .OrderByDesc(order => order.PurchaseDate);

            filter(query);

            var orders = query.Execute().ToList();
            if (orders.Count == 0)
                return XlsxSerializer.Instance.Serialize(new List<OrderExportXlsModel>());

            var orderIds = orders.Select(order => (long?)order.Id).ToList();
            var items = _db.SelectFrom<IOrderItem>()
                .Where(item => item.PurchaseOrderId.InCsv(orderIds))
                .Execute()
                .OrderBy(item => item.Id)
                .ToList();

            var itemIds = items.Select(item => item.Id).ToList();

            var batchAssignments = itemIds.Count == 0
                ? new List<IOrderItemMaterialBatch>()
                : _db.SelectFrom<IOrderItemMaterialBatch>()
                    .Join(assignment => assignment.MaterialBatch)
                    .Join(assignment => assignment.User)
                    .Where(assignment => assignment.OrderItemId.InCsv(itemIds))
                    .Execute()
                    .OrderBy(assignment => assignment.AssignmentDt)
                    .ToList();

            var priceElements = _db.SelectFrom<IOrderPriceElement>()
                .Where(element => element.PurchaseOrderId.InCsv(orderIds.Select(i => i.Value)))
                .Execute()
                .OrderBy(element => element.Id)
                .ToList();

            var itemsByOrderId = items.ToLookup(item => item.PurchaseOrderId ?? 0);
            var assignmentsByItemId = batchAssignments.ToLookup(assignment => assignment.OrderItemId);
            var priceElementsByOrderId = priceElements.ToLookup(element => element.PurchaseOrderId);
            var exportRows = orders.Select(order => MapOrder(
                    order,
                    itemsByOrderId[order.Id].ToList(),
                    assignmentsByItemId,
                    priceElementsByOrderId[order.Id].ToList()))
                .ToList();

            return XlsxSerializer.Instance.Serialize(exportRows);
        }

        public byte[] ExportAggregated(
            AggregatedResultModel aggregation,
            string aggregationName,
            IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            if (aggregation == null)
                throw new ArgumentNullException(nameof(aggregation));

            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Agregace");

                sheet.Cells[1, 1].Value = aggregation.RowsTitle;

                for (var columnIndex = 0; columnIndex < aggregation.Columns.Count; columnIndex++)
                    sheet.Cells[1, columnIndex + 2].Value = aggregation.Columns[columnIndex];

                for (var rowIndex = 0; rowIndex < aggregation.Rows.Count; rowIndex++)
                {
                    var row = aggregation.Rows[rowIndex];
                    sheet.Cells[rowIndex + 2, 1].Value = row;

                    for (var columnIndex = 0; columnIndex < aggregation.Columns.Count; columnIndex++)
                    {
                        sheet.Cells[rowIndex + 2, columnIndex + 2].Value =
                            ToCellValue(aggregation.Read(row, aggregation.Columns[columnIndex]));
                    }
                }

                sheet.Row(1).Style.Font.Bold = true;
                sheet.Column(1).Style.Font.Bold = true;
                sheet.View.FreezePanes(2, 2);

                if (sheet.Dimension != null)
                    sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

                var infoSheet = package.Workbook.Worksheets.Add("Popis");
                infoSheet.Cells["A1"].Value = "Typ agregace";
                infoSheet.Cells["B1"].Value = aggregationName;
                infoSheet.Cells["A2"].Value = "Vygenerováno";
                infoSheet.Cells["B2"].Value = DateTime.Now.ToString("d. M. yyyy HH:mm", _czechCulture);
                infoSheet.Cells["A3"].Value = "Legenda";
                infoSheet.Cells["B3"].Value = aggregation.Legend;
                infoSheet.Cells["B3"].Style.WrapText = true;

                var infoRow = 4;
                foreach (var parameterValue in parameterValues ?? Enumerable.Empty<KeyValuePair<string, string>>())
                {
                    infoSheet.Cells[infoRow, 1].Value = parameterValue.Key;
                    infoSheet.Cells[infoRow, 2].Value = parameterValue.Value;
                    infoRow++;
                }

                infoSheet.Column(1).Style.Font.Bold = true;
                infoSheet.Column(1).AutoFit();
                infoSheet.Column(2).Width = 100;

                return package.GetAsByteArray();
            }
        }

        private static object ToCellValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            var groupSeparator = _czechCulture.NumberFormat.NumberGroupSeparator;
            var normalized = value.Trim()
                .Replace("\u00A0", groupSeparator)
                .Replace("\u202F", groupSeparator)
                .Replace(" ", groupSeparator);

            if (decimal.TryParse(normalized, NumberStyles.Number, _czechCulture, out var number))
                return number;

            return value;
        }

        private static OrderExportXlsModel MapOrder(
            IPurchaseOrder order,
            IReadOnlyCollection<IOrderItem> items,
            ILookup<long, IOrderItemMaterialBatch> assignmentsByItemId,
            IReadOnlyCollection<IOrderPriceElement> priceElements)
        {
            var discounts = new[] { order.DiscountsText, order.PercentDiscountText }
                .Concat(priceElements.Select(element => element.Title))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return new OrderExportXlsModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                ErpOrderId = order.ErpOrderId,
                ErpId = order.ErpId,
                ErpName = order.Erp?.Description,
                PurchaseDate = order.PurchaseDate,
                BuyDate = order.BuyDate,
                DueDate = order.DueDate,
                ErpStatusName = order.ErpStatusName,
                OrderStatusName = order.OrderStatus?.Name,
                CustomerName = order.CustomerName,
                CustomerErpUid = order.CustomerErpUid,
                CustomerEmail = order.CustomerEmail,
                CompanyRegId = order.CompanyRegId,
                VatId = order.VatId,
                InvoiceAddress = FormatAddress(order.InvoiceAddress),
                DeliveryAddress = FormatAddress(order.DeliveryAddress),
                DeliveryPhone = order.DeliveryAddress?.Phone,
                Price = order.Price,
                PriceWithVat = order.PriceWithVat,
                Currency = order.Currency?.Symbol,
                VariableSymbol = order.VarSymbol,
                InvoiceId = order.InvoiceId,
                PreInvoiceId = order.PreInvoiceId,
                PaymentMethodName = order.PaymentMethodName,
                TaxedPaymentCost = order.TaxedPaymentCost,
                PaymentPairingUser = order.PaymentPairingUser?.EMail,
                PaymentPairingDt = order.PaymentPairingDt,
                PaymentSource = order.Payment?.PaymentSource?.Description,
                PaymentDt = order.Payment?.PaymentDt,
                PaymentAmount = order.Payment?.Value,
                PaymentCurrency = order.Payment?.Currency?.Symbol,
                PaymentVariableSymbol = order.Payment?.VariableSymbol,
                PaymentSender = order.Payment?.SenderName,
                PaymentMessage = order.Payment?.Message,
                ShippingMethodName = order.ShippingMethodName,
                TaxedShippingCost = order.TaxedShippingCost,
                PackingUser = order.PackingUser?.EMail,
                PackingDt = order.PackingDt,
                TotalWeightKg = items.Sum(item => item.Weight ?? 0m),
                Items = FormatItems(items),
                BatchAssignments = FormatBatchAssignments(items, assignmentsByItemId),
                Discounts = string.Join("; ", discounts),
                PriceElements = FormatPriceElements(priceElements),
                CustomerNote = order.CustomerNote,
                InternalNote = order.InternalNote,
                InsertUser = order.InsertUser?.EMail,
                InsertDt = order.InsertDt,
                ErpLastChange = order.ErpLastChange,
                ReturnDt = order.ReturnDt
            };
        }

        private static string FormatAddress(IAddress address)
        {
            if (address == null)
                return null;

            var name = string.Join(" ", new[] { address.CompanyName, address.FirstName, address.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
            var streetNumber = string.Join("/", new[] { address.DescriptiveNumber, address.OrientationNumber }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
            var street = string.Join(" ", new[] { address.Street, streetNumber }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
            var city = string.Join(" ", new[] { address.Zip, address.City }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            return string.Join(", ", new[] { name, street, city, address.Country }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        private static string FormatItems(IEnumerable<IOrderItem> items)
        {
            return string.Join("; ", items.Select(item =>
            {
                var kitPrefix = item.KitParentId == null ? string.Empty : "↳ ";
                var weight = item.Weight == null ? string.Empty : $", {item.Weight.Value.ToString("0.###", _czechCulture)} kg";
                return $"{kitPrefix}{item.Quantity.ToString("0.###", _czechCulture)}× {item.PlacedName} ({item.TaxedPrice.ToString("0.00", _czechCulture)} s DPH{weight})";
            }));
        }

        private static string FormatBatchAssignments(
            IEnumerable<IOrderItem> items,
            ILookup<long, IOrderItemMaterialBatch> assignmentsByItemId)
        {
            return string.Join("; ", items.SelectMany(item => assignmentsByItemId[item.Id]
                .Select(assignment =>
                    $"{assignment.Quantity.ToString("0.###", _czechCulture)}× {assignment.MaterialBatch?.BatchNumber ?? "bez čísla"}"
                    + $" ({item.PlacedName}, {assignment.User?.EMail ?? "neznámý uživatel"}, {assignment.AssignmentDt:d. M. yyyy})")));
        }

        private static string FormatPriceElements(IEnumerable<IOrderPriceElement> priceElements)
        {
            return string.Join("; ", priceElements.Select(element =>
            {
                var price = element.Price == null
                    ? string.Empty
                    : $": {element.Price.Value.ToString("0.00", _czechCulture)}";
                var tax = element.Tax == null
                    ? string.Empty
                    : $" + DPH {element.Tax.Value.ToString("0.00", _czechCulture)}";
                return $"{element.Title ?? element.TypeName}{price}{tax}";
            }));
        }
    }
}
