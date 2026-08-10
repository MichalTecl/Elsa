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

namespace Elsa.App.OrdersInfo
{
    [Controller("ordersInfo")]
    public class OrdersInfoController : ElsaControllerBase
    {
        private readonly OrdersInfoRepository _orderInfoRepository;
        private readonly IDatabase _db;

        public OrdersInfoController(IWebSession webSession, ILog log, OrdersInfoRepository orderInfoRepository, IDatabase db) : base(webSession, log)
        {
            _orderInfoRepository = orderInfoRepository;
            _db = db;
        }

        public List<OrderInfoModel> Query(OrderQueryModel query)
        {
            EnsureUserRight(OrdersInfoUserRights.OrdersInfoAppView);

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
            EnsureUserRight(OrdersInfoUserRights.OrdersInfoAppView);
            return _orderInfoRepository.GetPlacedItemNames();
        }

        public IList<string> GetErpStatuses()
        {
            EnsureUserRight(OrdersInfoUserRights.OrdersInfoAppView);
            return _orderInfoRepository.GetErpStatuses();
        }
    }
}
