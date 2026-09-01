using Elsa.App.OrdersInfo.Model;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Commerce;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Aggregations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.App.OrdersInfo.App
{
    public class OrdersInfoRepository
    {
        private readonly IDatabase _db;
        private readonly ISession _session;
        private readonly ICache _cache;

        public OrdersInfoRepository(IDatabase db, ISession session, ICache cache)
        {
            _db = db;
            _session = session;
            _cache = cache;
        }

        public OrderQueryResultModel Load(int skip, int take, Action<IQueryBuilder<IPurchaseOrder>> query)
        {
            var q = CreateOrdersQuery()
                .OrderByDesc(order => order.PurchaseDate)
                .Take(take)
                .Skip(skip);

            query(q);

            var raw = q.Execute();
            var result = _db.AggregateFrom<IPurchaseOrder>()
                .Where(order => order.ProjectId == _session.Project.Id)
                .Apply(query)
                .GroupAll<OrderQueryResultModel>()
                .Bind(order => order.Id.Count(), (summary, value) => summary.TotalCount = value)
                .Bind(order => order.PriceWithVat.Sum(), (summary, value) => summary.TotalPriceWithVat = value)
                .Execute()
                .Single();

            result.Orders = raw.Select(MapOrderModel).ToList();

            return result;
        }

        private IQueryBuilder<IPurchaseOrder> CreateOrdersQuery()
        {
            return _db.SelectFrom<IPurchaseOrder>()
                .Where(order => order.ProjectId == _session.Project.Id);
        }

        public OrderInfoModel MapOrderModel(IPurchaseOrder source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            IEnumerable<string> GetDiscounts()
            {
                yield return source.DiscountsText;
                yield return source.PercentDiscountText;

                foreach (var pe in source.PriceElements)
                    yield return pe.Title;
            }

            return new OrderInfoModel
            {
                OrderId = source.Id,
                OrderNumber = source.OrderNumber,
                PriceWithVat = source.PriceWithVat,
                PurchaseDate = source.PurchaseDate,
                ErpStatusName = source.ErpStatusName,
                ShippingMethodName = source.ShippingMethodName,
                PaymentMethodName = source.PaymentMethodName,
                CustomerName = source.CustomerName,
                CustomerErpUid = source.CustomerErpUid,
                Discounts = string.Join(", ", GetDiscounts()
                    .Where(discount => !string.IsNullOrWhiteSpace(discount))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
            };
        }

        public IList<string> GetErpStatuses()
        {
            return _cache.ReadThrough($"ordinf_allErpStatuses_{_session.Project.Id}",
                TimeSpan.FromHours(1),
                () => _db.Sql()
                .ExecuteWithParams(@"SELECT DISTINCT ErpStatusName
                                       FROM PurchaseOrder
                                      WHERE ProjectId = {0}
                                        AND ErpStatusName IS NOT NULL
                                      ORDER BY ErpStatusName", _session.Project.Id)
                .MapRows(r => r.GetString(0)));
        }

        public IList<PaymentMethodInfoModel> GetPaymentMethods()
        {
            return _cache.ReadThrough($"ordinf_paymentMethodsWithLastUse_{_session.Project.Id}",
                TimeSpan.FromHours(1),
                () =>
                {
                    var activeSince = DateTime.Now.AddYears(-1);
                    var paymentMethods = _db.AggregateFrom<IPurchaseOrder>()
                        .Where(order => order.ProjectId == _session.Project.Id)
                        .GroupBy<PaymentMethodInfoModel>(order => order.PaymentMethodName, method => method.Name)
                        .Bind(order => order.PurchaseDate.Max(), (method, value) => method.LastUsedDt = value)
                        .Execute()
                        .Where(method => !string.IsNullOrWhiteSpace(method.Name))
                        .ToList();

                    foreach (var paymentMethod in paymentMethods)
                    {
                        paymentMethod.IsActive = paymentMethod.LastUsedDt >= activeSince;
                        paymentMethod.LastUsedDtText = paymentMethod.LastUsedDt.ToString("d. M. yyyy");
                    }

                    return paymentMethods
                        .OrderBy(method => method.IsActive ? 0 : 1)
                        .ThenBy(method => method.Name, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();
                });
        }

        public IList<string> GetPlacedItemNames()
        {
            return _cache.ReadThrough($"ordinf_allPlacedItemNames_{_session.Project.Id}",
                TimeSpan.FromHours(1),
                () => _db.Sql()
                    .ExecuteWithParams(@"SELECT DISTINCT oi.PlacedName
                                           FROM OrderItem oi
                                           JOIN PurchaseOrder po ON po.Id = oi.PurchaseOrderId
                                          WHERE po.ProjectId = {0}
                                            AND oi.PlacedName IS NOT NULL
                                          ORDER BY oi.PlacedName", _session.Project.Id)
                    .MapRows(r => r.GetString(0)));
        }
    }
}
