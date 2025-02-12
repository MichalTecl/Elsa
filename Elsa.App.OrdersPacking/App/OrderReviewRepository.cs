using Elsa.App.OrdersPacking.Entities;
using Elsa.App.OrdersPacking.Model;
using Elsa.Commerce.Core;
using Elsa.Common.Interfaces;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;

namespace Elsa.App.OrdersPacking.App
{
    public class OrderReviewRepository
    {
        private readonly IDatabase _database;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ISession _session;

        public OrderReviewRepository(IDatabase database, IPurchaseOrderRepository purchaseOrderRepository, ISession session)
        {
            _database = database;
            _purchaseOrderRepository = purchaseOrderRepository;
            _session = session;
        }
                
        public List<OrderReviewRow> GetItemsToReview()
        {
            return _database
                .Sql()
                .Call("GetOrdersToReview")
                .WithParam("@projectId", _session.Project.Id)
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
