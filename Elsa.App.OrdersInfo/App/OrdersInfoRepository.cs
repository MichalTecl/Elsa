using Elsa.App.OrdersInfo.Model;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Commerce;
using Robowire.RobOrm.Core;
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

        public List<OrderInfoModel> Load(int skip, int take, Action<IQueryBuilder<IPurchaseOrder>> query)
        {            
            var q = _db.SelectFrom<IPurchaseOrder>()
                           .Where(o => o.ProjectId == _session.Project.Id)
                           .OrderByDesc(o => o.PurchaseDate)
                           .Take(take)
                           .Skip(skip);

            query(q);

            var raw = q.Execute();

            return new List<OrderInfoModel>(raw.Select(MapOrderModel));
        }

        public OrderInfoModel MapOrderModel(IPurchaseOrder source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            IEnumerable<string> GetDiscounts()
            {
                yield return source.DiscountsText;
                yield return source.PercentDiscountText;

                foreach (var pe in source.PriceElements.Where(pe => pe.TypeName != "shipping" && pe.TypeName != "payment"))
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
                Discounts = string.Join(", ", GetDiscounts().Where(d => !string.IsNullOrWhiteSpace(d)).Distinct())
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
