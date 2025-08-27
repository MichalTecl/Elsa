using Elsa.App.OrdersPacking.App;
using Elsa.App.OrdersPacking.Model;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;

namespace Elsa.App.OrdersPacking
{
    [Controller("OrdersReview")]
    public class OrdersReviewController : ElsaControllerBase
    {
        private readonly OrderReviewRepository _orderReviewRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;

        public OrdersReviewController(IWebSession webSession, ILog log, OrderReviewRepository orderReviewRepository, IPurchaseOrderRepository purchaseOrderRepository) : base(webSession, log)
        {
            _orderReviewRepository = orderReviewRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
        }

        protected override void OnBeforeCall(RequestContext context)
        {
            base.OnBeforeCall(context);
            EnsureUserRight(OrdersPackingUserRights.OrdersReview);
        }

        public List<OrderReviewRow> GetOrders() => _orderReviewRepository.GetItemsToReview();

        public List<OrderReviewRow> MarkReviewed(long orderId)
        {
            _orderReviewRepository.MarkReviewed(orderId, $"{nameof(OrdersReviewController)}.{nameof(MarkReviewed)}");
            return GetOrders();
        }

        public List<IOrderItem> GetOrderItems(long orderId) 
        {           
            var order = _purchaseOrderRepository.GetOrder(orderId);

            return order.Items.ToList();
        }

        public int BulkReview()
        {
            return _orderReviewRepository.BulkReview();
        }
    }
}
