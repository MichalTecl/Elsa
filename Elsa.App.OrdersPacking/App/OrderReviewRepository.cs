using Elsa.App.OrdersPacking.Entities;
using Elsa.App.OrdersPacking.Model;
using Elsa.Commerce.Core;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.App.OrdersPacking.App
{
    public class OrderReviewRepository
    {
        private readonly IDatabase _database;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ISession _session;
        private readonly IKitProductRepository _kitProductRepository;
        private readonly ICache _cache;

        public OrderReviewRepository(IDatabase database, IPurchaseOrderRepository purchaseOrderRepository, ISession session, IKitProductRepository kitProductRepository, ICache cache)
        {
            _database = database;
            _purchaseOrderRepository = purchaseOrderRepository;
            _session = session;
            _kitProductRepository = kitProductRepository;
            _cache = cache;
        }

        public List<OrderReviewRow> GetItemsToReview()
        {
            var invalidKitNote = _cache.ReadThrough(
                $"invalidKitnoteOrders_{_session.Project.Id}",
                TimeSpan.FromMinutes(2),
                () => _kitProductRepository
                        .ParseKitNotes(null)
                        .Where(r => r.KitDefinitionId == null
                            || r.SelectionGroupId == null
                            || r.SelectionGroupItemId == null)
                        .Select(r => r.OrderId)
                        .ToList()
                );

            return _database
                .Sql()
                .Call("GetOrdersToReview")
                .WithParam("@projectId", _session.Project.Id)
                .WithStructuredParam("@invalidKitNoteOrderIds", "IntTable", invalidKitNote, new[] { "Id" }, i => new object[] { i } )
                .AutoMap<OrderReviewRow>();
        }

        public void MarkReviewed(long orderId, string note)
        {
            var record = _database.New<IOrderReviewResult>();
            record.OrderId = orderId;
            record.AuthorId = _session.User.Id;
            record.ReviewDt = DateTime.Now;
            record.Note = note;

            _database.Save(record);
        }
    }
}
