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

namespace Elsa.App.OrdersInfo
{
    [Controller("ordersInfo")]
    public class OrdersInfoController : ElsaControllerBase
    {
        private const string CRM_APP_RIGHT = "DistributorsApp";
        private const string COMGATE_TRANSACTIONS_URL = "https://portal.comgate.cz/cs/transakce";

        private readonly OrdersInfoRepository _orderInfoRepository;
        private readonly IDatabase _db;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ICustomerRepository _customerRepository;

        public OrdersInfoController(
            IWebSession webSession,
            ILog log,
            OrdersInfoRepository orderInfoRepository,
            IDatabase db,
            IPurchaseOrderRepository purchaseOrderRepository,
            ICustomerRepository customerRepository) : base(webSession, log)
        {
            _orderInfoRepository = orderInfoRepository;
            _db = db;
            _purchaseOrderRepository = purchaseOrderRepository;
            _customerRepository = customerRepository;
        }

        protected override void OnBeforeCall()
        {
            EnsureUserRight(OrdersInfoUserRights.OrdersInfoAppView);
        }

        public List<OrderInfoModel> Query(OrderQueryModel query)
        {
            query = query ?? new OrderQueryModel();

            var take = Math.Min(Math.Max(query.PageSize, 1), 200);
            var page = Math.Max(query.Page, 0);
            var skip = checked(page * take);


            var result = _orderInfoRepository.Load(skip, take,
                q => { 

                    if (!string.IsNullOrWhiteSpace(query.OrderNumber))
                        q.Where(o => o.OrderNumber.Like(query.OrderNumber.ToSqlLike()));

                    if (query.MinPurchaseDt != null)
                        q.Where(o => o.PurchaseDate >= query.MinPurchaseDt);

                    if (query.MaxPurchaseDt != null)
                        q.Where(o => o.PurchaseDate <= query.MaxPurchaseDt);

                    if (query.ErpStatuses?.Count > 0)
                        q.Where(o => o.ErpStatusName.InCsv(query.ErpStatuses));
                    
                    if (!string.IsNullOrWhiteSpace(query.ContainsPlacedItemWildcard))
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

                    if (!string.IsNullOrWhiteSpace(query.PaymentMethodNameWildcard))
                        q.Where(o => o.PaymentMethodName.Like(query.PaymentMethodNameWildcard.ToSqlLike()));

                });

            return result;
        }

        public IList<string> GetPlacedItemNames()
        {
            return _orderInfoRepository.GetPlacedItemNames();
        }

        public IList<string> GetErpStatuses()
        {
            return _orderInfoRepository.GetErpStatuses();
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
            var assignmentsByItemId = itemIds.Count == 0
                ? new Dictionary<long, List<IOrderItemMaterialBatch>>()
                : _db.SelectFrom<IOrderItemMaterialBatch>()
                    .Join(assignment => assignment.MaterialBatch)
                    .Join(assignment => assignment.User)
                    .Where(assignment => assignment.OrderItemId.InCsv(itemIds))
                    .Execute()
                    .GroupBy(assignment => assignment.OrderItemId)
                    .ToDictionary(group => group.Key, group => group.OrderBy(assignment => assignment.AssignmentDt).ToList());

            return order.Items
                .Where(item => item.KitParentId == null)
                .OrderBy(item => item.Id)
                .Select(item => MapOrderItem(item, assignmentsByItemId))
                .ToList();
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
            IOrderItem item,
            IReadOnlyDictionary<long, List<IOrderItemMaterialBatch>> assignmentsByItemId)
        {
            var result = new OrderItemInfoModel
            {
                ItemId = item.Id,
                PlacedName = item.PlacedName,
                Quantity = item.Quantity,
                PriceWithVat = item.TaxedPrice
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
                    AssignedBy = assignment.User?.EMail ?? "Neznámý uživatel"
                }));
            }

            result.Children.AddRange(item.KitChildren
                .OrderBy(child => child.KitItemIndex)
                .ThenBy(child => child.Id)
                .Select(child => MapOrderItem(child, assignmentsByItemId)));

            return result;
        }
    }
}
