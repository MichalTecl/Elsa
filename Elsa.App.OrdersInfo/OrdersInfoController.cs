using Elsa.App.OrdersInfo.App;
using Elsa.App.OrdersInfo.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using Robowire.RobOrm.Core;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using System.Linq;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using System.Net;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.App.OrdersPacking.Entities;

namespace Elsa.App.OrdersInfo
{
    [Controller("ordersInfo")]
    public class OrdersInfoController : ElsaControllerBase
    {
        private const string CRM_APP_RIGHT = "DistributorsApp";
        private const string COMGATE_TRANSACTIONS_URL = "https://portal.comgate.cz/cs/transakce";
        private const string DPD_SHIPMENTS_URL = "https://shipping.dpdgroup.com/shipments";
        private const string PACKETA_PACKETS_URL = "https://client.packeta.com/cs/packets/list";

        private readonly OrdersInfoRepository _orderInfoRepository;
        private readonly IDatabase _db;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly OrdersInfoXlsExporter _xlsExporter;
        private readonly OrderItemBatchAssignmentEditor _batchAssignmentEditor;

        public OrdersInfoController(
            IWebSession webSession,
            ILog log,
            OrdersInfoRepository orderInfoRepository,
            IDatabase db,
            IPurchaseOrderRepository purchaseOrderRepository,
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            OrdersInfoXlsExporter xlsExporter,
            OrderItemBatchAssignmentEditor batchAssignmentEditor) : base(webSession, log)
        {
            _orderInfoRepository = orderInfoRepository;
            _db = db;
            _purchaseOrderRepository = purchaseOrderRepository;
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _xlsExporter = xlsExporter;
            _batchAssignmentEditor = batchAssignmentEditor;
        }

        protected override void OnBeforeCall()
        {
            EnsureUserRight(OrdersInfoUserRights.OrdersInfoAppView);
        }

        public OrderQueryResultModel Query(OrderQueryModel query)
        {
            query = PrepareQuery(query);

            var take = Math.Min(Math.Max(query.PageSize, 1), 200);
            var page = Math.Max(query.Page, 0);
            var skip = checked(page * take);

            return _orderInfoRepository.Load(skip, take, CreateOrderFilter(query));
        }

        public FileResult Export(OrderQueryModel query)
        {
            query = PrepareQuery(query);
            var bytes = _xlsExporter.Export(CreateOrderFilter(query));
            return new FileResult($"Objednavky_{DateTime.Now:yyyyMMdd_HHmm}.xlsx", bytes);
        }

        private static OrderQueryModel PrepareQuery(OrderQueryModel query)
        {
            query = query ?? new OrderQueryModel();
            query.OrderNumber = NormalizeWildcard(query.OrderNumber);
            query.ContainsPlacedItemWildcard = NormalizeWildcard(query.ContainsPlacedItemWildcard);
            query.MaterialBatchNumberWildcard = NormalizeWildcard(query.MaterialBatchNumberWildcard);
            query.CustomerNameWildcard = NormalizeWildcard(query.CustomerNameWildcard);
            query.ShipmentMethodNameWildcard = NormalizeWildcard(query.ShipmentMethodNameWildcard);
            query.DiscountTextWildcard = NormalizeWildcard(query.DiscountTextWildcard);
            return query;
        }

        private Action<IQueryBuilder<IPurchaseOrder>> CreateOrderFilter(OrderQueryModel query)
        {
            return q =>
            {
                if (!string.IsNullOrWhiteSpace(query.OrderNumber))
                    q.Where(o => o.OrderNumber.Like(query.OrderNumber.ToSqlLike()));

                if (query.MinPurchaseDt != null)
                    q.Where(o => o.PurchaseDate >= query.MinPurchaseDt);

                if (query.MaxPurchaseDt != null)
                    q.Where(o => o.PurchaseDate <= query.MaxPurchaseDt);

                if (query.ErpStatuses?.Count > 0)
                    q.Where(o => o.ErpStatusName.InCsv(query.ErpStatuses));

                if (query.OrderStatusId != null)
                {
                    var filteredOrderStatusId = query.OrderStatusId.Value;
                    q.Where(o => o.OrderStatusId == filteredOrderStatusId);
                }

                if (!string.IsNullOrWhiteSpace(query.MaterialBatchNumberWildcard))
                {
                    var batchQuery = _db.SelectFrom<IOrderItemMaterialBatch>()
                        .Join(assignment => assignment.MaterialBatch)
                        .Join(assignment => assignment.OrderItem)
                        .Join(assignment => assignment.OrderItem.KitParent)
                        .Join(assignment => assignment.OrderItem.PurchaseOrder)
                        .Where(assignment => assignment.MaterialBatch.BatchNumber.Like(
                            query.MaterialBatchNumberWildcard.ToSqlLike()));

                    if (!string.IsNullOrWhiteSpace(query.ContainsPlacedItemWildcard))
                    {
                        batchQuery.Where(assignment =>
                            assignment.OrderItem.PlacedName.Like(query.ContainsPlacedItemWildcard.ToSqlLike())
                            || assignment.OrderItem.KitParent.PlacedName.Like(query.ContainsPlacedItemWildcard.ToSqlLike()));
                    }

                    q.Where(order => order.Id.InSubquery(batchQuery
                        .Transform(assignment => assignment.OrderItem.PurchaseOrder.Id)));
                }
                else if (!string.IsNullOrWhiteSpace(query.ContainsPlacedItemWildcard))
                {
                    q.Where(o => o.Id.InSubquery(_db.SelectFrom<IOrderItem>()
                        .Join(i => i.KitChildren)
                        .Join(i => i.PurchaseOrder)
                        .Where(i => i.PlacedName.Like(query.ContainsPlacedItemWildcard.ToSqlLike())
                                    || i.KitChildren.Each().PlacedName.Like(query.ContainsPlacedItemWildcard.ToSqlLike()))
                        .Transform(i => i.PurchaseOrder.Id)));
                }

                if (!string.IsNullOrWhiteSpace(query.CustomerNameWildcard))
                    q.Where(o => o.CustomerName.Like(query.CustomerNameWildcard.ToSqlLike()));

                if (!string.IsNullOrWhiteSpace(query.ShipmentMethodNameWildcard))
                    q.Where(o => o.ShippingMethodName.Like(query.ShipmentMethodNameWildcard.ToSqlLike()));

                if (query.PaymentMethodNames?.Count > 0)
                    q.Where(o => o.PaymentMethodName.InCsv(query.PaymentMethodNames));

                if (!string.IsNullOrWhiteSpace(query.DiscountTextWildcard))
                {
                    q.Where(o => o.Id.InSubquery(_db.SelectFrom<IOrderPriceElement>()
                        .Where(element => element.Title.Like(query.DiscountTextWildcard.ToSqlLike()))
                        .Transform(element => element.PurchaseOrderId)));
                }
            };
        }

        private static string NormalizeWildcard(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var normalizedValue = value.Trim();
            if (normalizedValue.Length >= 2
                && normalizedValue[0] == '"'
                && normalizedValue[normalizedValue.Length - 1] == '"')
            {
                var unquotedValue = normalizedValue.Substring(1, normalizedValue.Length - 2);
                return string.IsNullOrWhiteSpace(unquotedValue) ? null : unquotedValue;
            }

            return normalizedValue.IndexOf('*') >= 0
                ? normalizedValue
                : $"*{normalizedValue}*";
        }

        public IList<string> GetPlacedItemNames()
        {
            return _orderInfoRepository.GetPlacedItemNames();
        }

        public IList<string> GetErpStatuses()
        {
            return _orderInfoRepository.GetErpStatuses();
        }

        public IList<PaymentMethodInfoModel> GetPaymentMethods()
        {
            return _orderInfoRepository.GetPaymentMethods();
        }

        public List<OrderNoteModel> GetOrderNotes(long orderId)
        {
            var order = GetOrder(orderId);

            var result = new List<OrderNoteModel>();

            if (!string.IsNullOrWhiteSpace(order.InternalNote))
                result.Add(new OrderNoteModel
                {
                    NoteType = "Interní poznámka",
                    NoteText = order.InternalNote,
                });

            if (!string.IsNullOrWhiteSpace(order.CustomerNote))
                result.Add(new OrderNoteModel
                {
                    NoteType = "Poznámka od zákazníka",
                    NoteText = order.CustomerNote,
                });

            return result;
        }

        public List<OrderItemInfoModel> GetOrderItems(long orderId)
        {
            var order = GetOrder(orderId);
            var itemIds = GetAllOrderItems(order.Items).Select(item => item.Id).Distinct().ToList();
            var assignments = itemIds.Count == 0
                ? new List<IOrderItemMaterialBatch>()
                : _db.SelectFrom<IOrderItemMaterialBatch>()
                    .Join(assignment => assignment.MaterialBatch)
                    .Join(assignment => assignment.User)
                    .Where(assignment => assignment.OrderItemId.InCsv(itemIds))
                    .Execute()
                    .ToList();
            var assignmentsByItemId = assignments
                .GroupBy(assignment => assignment.OrderItemId)
                .ToDictionary(group => group.Key, group => group.OrderBy(assignment => assignment.AssignmentDt).ToList());
            var editBlockReason = _batchAssignmentEditor.GetEditBlockReason(order);
            var batchAssignmentDateNotice = _batchAssignmentEditor.GetBatchAssignmentDateNotice(order, DateTime.Now);

            return order.Items
                .Where(item => item.KitParentId == null)
                .OrderBy(item => item.Id)
                .Select(item => MapOrderItem(
                    order,
                    item,
                    assignmentsByItemId,
                    editBlockReason,
                    batchAssignmentDateNotice))
                .ToList();
        }

        public List<OrderItemInfoModel> SaveOrderItemBatchAssignments(
            OrderItemBatchAssignmentChangeRequest request)
        {
            WebSession.EnsureUserRight(OrdersInfoUserRights.EditOrderItemBatchAssignments);
            _batchAssignmentEditor.Apply(request);
            return GetOrderItems(request.OrderId);
        }

        public string GetCrmLink(long orderId)
        {
            if (!WebSession.HasUserRight(CRM_APP_RIGHT))
                throw new UnauthorizedAccessException("Uživatel nemá oprávnění otevřít CRM aplikaci");

            var order = GetOrder(orderId);

            if (string.IsNullOrWhiteSpace(order.CustomerErpUid)
                || !order.CustomerErpUid.StartsWith("C", StringComparison.Ordinal))
                throw new ArgumentException("Objednávka není přiřazena k zákazníkovi v CRM");

            var customer = _customerRepository.GetCustomerByErpUid(order.CustomerErpUid)
                           ?? throw new ArgumentException("Zákazník nebyl v CRM nalezen");

            return $"/UI/DistributorsApp/DistributorsAppPage.html?customerId={customer.Id}";
        }

        public PaymentDetailModel GetPaymentDetail(long orderId)
        {
            var order = _db.SelectFrom<IPurchaseOrder>()
                .Join(item => item.Currency)
                .Join(item => item.PaymentPairingUser)
                .Join(item => item.Payment)
                .Join(item => item.Payment.PaymentSource)
                .Join(item => item.Payment.Currency)
                .Where(item => item.Id == orderId)
                .Where(item => item.ProjectId == WebSession.Project.Id)
                .Execute()
                .FirstOrDefault() ?? throw new ArgumentException("Objednávka nenalezena");

            var result = new PaymentDetailModel
            {
                PaymentMethodName = order.PaymentMethodName,
                IsPayOnDelivery = order.IsPayOnDelivery,
                TaxedPaymentCost = order.TaxedPaymentCost,
                OrderCurrencySymbol = order.Currency?.Symbol,
                HasPairingInfo = order.PaymentPairingUserId != null || order.PaymentPairingDt != null,
                PaymentPairingUser = order.PaymentPairingUser?.EMail,
                PaymentPairingDt = order.PaymentPairingDt,
                HasPayment = order.PaymentId != null && order.Payment != null
            };

            if (!string.IsNullOrWhiteSpace(order.PaymentMethodName)
                && order.PaymentMethodName.IndexOf("kartou", StringComparison.OrdinalIgnoreCase) >= 0)
                result.ComgateUrl = BuildComgateUrl(order);

            if (result.HasPairingInfo && string.IsNullOrWhiteSpace(result.PaymentPairingUser))
                result.PaymentPairingUser = "Neznámý uživatel";

            if (result.HasPayment)
            {
                result.Payment = new PaymentInfoModel
                {
                    PaymentId = order.Payment.Id,
                    SourceName = order.Payment.PaymentSource?.Description ?? "Neznámý zdroj",
                    PaymentDt = order.Payment.PaymentDt,
                    Amount = order.Payment.Value,
                    CurrencySymbol = order.Payment.Currency?.Symbol,
                    VariableSymbol = order.Payment.VariableSymbol,
                    Message = order.Payment.Message,
                    SenderName = order.Payment.SenderName
                };
            }

            return result;
        }

        public ShippingDetailModel GetShippingDetail(long orderId)
        {
            var order = _db.SelectFrom<IPurchaseOrder>()
                .Join(item => item.Currency)
                .Join(item => item.DeliveryAddress)
                .Join(item => item.PackingUser)
                .Where(item => item.Id == orderId)
                .Where(item => item.ProjectId == WebSession.Project.Id)
                .Execute()
                .FirstOrDefault() ?? throw new ArgumentException("Objednávka nenalezena");

            var orderItems = _db.SelectFrom<IOrderItem>()
                .Where(item => item.PurchaseOrderId == orderId)
                .Execute()
                .OrderBy(item => item.Id)
                .ToList();

            var result = new ShippingDetailModel
            {
                ShippingMethodName = order.ShippingMethodName,
                TaxedShippingCost = order.TaxedShippingCost,
                OrderCurrencySymbol = order.Currency?.Symbol,
                HasPackingInfo = order.PackingUserId != null || order.PackingDt != null,
                PackingUser = order.PackingUser?.EMail,
                PackingDt = order.PackingDt,
                TotalItemsWeightKg = orderItems.Sum(item => item.Weight ?? 0m),
                ItemWeights = orderItems.Select(item => new ShippingItemWeightModel
                {
                    ItemName = item.PlacedName,
                    WeightKg = item.Weight
                }).ToList()
            };

            if (order.DeliveryAddress != null)
            {
                result.DeliveryAddress = new ShippingAddressInfoModel
                {
                    CompanyName = order.DeliveryAddress.CompanyName,
                    FirstName = order.DeliveryAddress.FirstName,
                    LastName = order.DeliveryAddress.LastName,
                    Street = order.DeliveryAddress.Street,
                    DescriptiveNumber = order.DeliveryAddress.DescriptiveNumber,
                    OrientationNumber = order.DeliveryAddress.OrientationNumber,
                    City = order.DeliveryAddress.City,
                    Zip = order.DeliveryAddress.Zip,
                    Country = order.DeliveryAddress.Country,
                    Phone = order.DeliveryAddress.Phone,
                    Note = order.DeliveryAddress.Note
                };
            }

            if (result.HasPackingInfo && string.IsNullOrWhiteSpace(result.PackingUser))
                result.PackingUser = "Neznámý uživatel";

            var isDpd = !string.IsNullOrWhiteSpace(order.ShippingMethodName)
                        && order.ShippingMethodName.IndexOf("DPD", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isDpd)
                result.DpdUrl = BuildDpdUrl(order.OrderNumber);
            else
                result.PacketaUrl = BuildPacketaUrl(order);

            return result;
        }

        public SysInfoModel GetOrderSysInfo(long orderId)
        {
            var order = GetOrder(orderId);

            var events = new List<OrderEventModel>();

            void AddEvent(DateTime? dateTime, string text, int? userId = null)
            {
                if (dateTime == null || string.IsNullOrWhiteSpace(text))
                    return;

                var user = userId == null
                    ? null
                    : _userRepository.GetUser(userId.Value)?.EMail;

                events.Add(new OrderEventModel
                {
                    Dt = dateTime.Value,
                    Text = text.Trim(),
                    User = user
                });
            }

            AddEvent(order.BuyDate, nameof(order.BuyDate));
            AddEvent(order.PurchaseDate, nameof(order.PurchaseDate));
            AddEvent(order.DueDate, nameof(order.DueDate));
            AddEvent(order.ErpLastChange, nameof(order.ErpLastChange));
            AddEvent(order.InsertDt, "Záznam vytvořen", order.InsertUserId);
            AddEvent(order.PaymentPairingDt, "Platba spárována", order.PaymentPairingUserId);
            AddEvent(order.PackingDt, "Zabaleno", order.PackingUserId);
            AddEvent(order.ReturnDt, "Vráceno");

            foreach (var blocker in _db.SelectFrom<IOrderProcessingBlocker>().Where(o => o.PurchaseOrderId == orderId).Execute())
                AddEvent(blocker.CreateDt, blocker.Message, blocker.AuthorId);

            foreach (var e in _db.SelectFrom<IOrderProcessingLog>().Where(l => l.PurchaseOrderId == orderId).Execute())
                AddEvent(e.ProcessDt, e.ProcessCode);

            foreach (var rev in _db.SelectFrom<IOrderReviewResult>().Where(r => r.OrderId == orderId).Execute())
                AddEvent(rev.ReviewDt, "Potvrzeno v 'Objednávky ke kontrole'", rev.AuthorId);

            var sortedEvents = events
                .OrderBy(orderEvent => orderEvent.Dt)
                .ThenBy(orderEvent => orderEvent.Text)
                .ToList();

            for (var index = 0; index < sortedEvents.Count; index++)
                sortedEvents[index].Id = index + 1;

            return new SysInfoModel
            {
                OrderId = orderId,
                Events = sortedEvents
            };
        }

        private static string BuildDpdUrl(string orderNumber)
        {
            return $"{DPD_SHIPMENTS_URL}?page=0&limit=10&parcelRef={WebUtility.UrlEncode(orderNumber)}";
        }

        private static string BuildPacketaUrl(IPurchaseOrder order)
        {
            var dateFrom = order.PurchaseDate.AddYears(-1).Date;
            var dateTo = order.PurchaseDate.AddYears(1).Date;
            var period = WebUtility.UrlEncode($"{dateFrom:dd.MM.yy}-{dateTo:dd.MM.yy}");

            return $"{PACKETA_PACKETS_URL}?locale=cs"
                   + "&list-perPage=50"
                   + $"&list-filter%5BdateStored%5D%5Bfrom%5D={period}"
                   + $"&list-filter%5Bnumber%5D={WebUtility.UrlEncode(order.OrderNumber)}";
        }

        private static string BuildComgateUrl(IPurchaseOrder order)
        {
            var dateFrom = order.PurchaseDate.AddYears(-1).Date;
            var dateTo = order.PurchaseDate.AddYears(1).Date.AddDays(1).AddMinutes(-1);
            var period = WebUtility.UrlEncode($"{dateFrom:dd.MM.yyyy HH:mm} - {dateTo:dd.MM.yyyy HH:mm}");
            var orderNumber = WebUtility.UrlEncode(order.OrderNumber);

            return $"{COMGATE_TRANSACTIONS_URL}?Transakce%5Bdatum%5D={period}"
                   + "&Transakce%5Bstatus%5D%5B%5D=PAYMENT_STAT_STATUS_PAID"
                   + "&Transakce%5Bstatus%5D%5B%5D=PAYMENT_STAT_STATUS_UNPAID"
                   + "&Transakce%5Bstatus%5D%5B%5D=PAYMENT_STAT_STATUS_PENDING"
                   + "&Transakce%5Bstatus%5D%5B%5D=PAYMENT_STAT_STATUS_PREVEDENA"
                   + "&Transakce%5Bstatus%5D%5B%5D=PAYMENT_STAT_STATUS_AUTHORIZED"
                   + "&Transakce%5BidAgmo%5D="
                   + $"&Transakce%5BidKlienta%5D={orderNumber}"
                   + "&Transakce%5Bvar_symbol%5D="
                   + "&Transakce%5Blabel%5D="
                   + "&Transakce%5Bkarta_cislo_zacatek%5D="
                   + "&Transakce%5Bkarta_cislo_konec%5D="
                   + "&Transakce%5Bmena%5D="
                   + "&Transakce%5Bcastka%5D="
                   + "&Transakce%5Bplatce%5D="
                   + "&yt0=";
        }

        private IPurchaseOrder GetOrder(long orderId)
        {
            return _purchaseOrderRepository.GetOrder(orderId)
                   ?? throw new ArgumentException("Objednávka nenalezena");
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

        private static OrderItemInfoModel MapOrderItem(
            IPurchaseOrder order,
            IOrderItem item,
            IReadOnlyDictionary<long, List<IOrderItemMaterialBatch>> assignmentsByItemId,
            string editBlockReason,
            string batchAssignmentDateNotice)
        {
            var result = new OrderItemInfoModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                ItemId = item.Id,
                PlacedName = item.PlacedName,
                Quantity = item.Quantity,
                PriceWithVat = item.TaxedPrice,
                BatchAssignmentsLocked = editBlockReason != null,
                BatchAssignmentsLockedReason = editBlockReason,
                BatchAssignmentDateNotice = batchAssignmentDateNotice
            };

            if (assignmentsByItemId.TryGetValue(item.Id, out var assignments))
            {
                result.BatchAssignments.AddRange(assignments.Select(assignment => new OrderItemBatchAssignmentInfoModel
                {
                    AssignmentId = assignment.Id,
                    MaterialBatchId = assignment.MaterialBatchId,
                    BatchNumber = assignment.MaterialBatch?.BatchNumber ?? "Bez čísla šarže",
                    Quantity = assignment.Quantity,
                    AssignmentDt = assignment.AssignmentDt,
                    AssignedBy = assignment.User?.EMail ?? "Neznámý uživatel",
                    CanDelete = editBlockReason == null,
                    DeleteBlockedReason = editBlockReason
                }));
            }

            result.Children.AddRange(item.KitChildren
                .OrderBy(child => child.KitItemIndex)
                .ThenBy(child => child.Id)
                .Select(child => MapOrderItem(
                    order,
                    child,
                    assignmentsByItemId,
                    editBlockReason,
                    batchAssignmentDateNotice)));

            return result;
        }
    }
}
