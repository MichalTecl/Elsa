using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm.Model;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.App.Crm.Model
{
    public class CustomerViewModel : ICommonCustomerInfo
    {
        public CustomerViewModel(CustomerOverview src, IProject project, IUserRepository userRepository)
        {
            CustomerId = src.CustomerId;
            Name = src.Name;
            Email = src.Email;
            IsRegistered = src.IsRegistered;
            IsNewsletterSubscriber = src.IsNewsletterSubscriber;
            IsDistributor = src.IsDistributor;

            TotalSpent = $"{StringUtil.FormatDecimal(src.TotalSpent)} {src.Currency}";

            var items = new List<CustomerRelatedItem>();
            
            if (src.Orders.Count > 10)
            {
                OmittedOrders = src.Orders.Count - 10;
            }

            for (var i = 0; i < Math.Min(src.Orders.Count, 10); i++)
            {
                var order = src.Orders[i];
                items.AddRange(CreateOrderItem(src, order, project));
            }
            
            foreach (var msg in src.Messages)
            {
                var author = userRepository.GetUserNick(msg.AuthorId);
                if (author.Equals(src.Nick, StringComparison.InvariantCultureIgnoreCase))
                {
                    author = userRepository.GetUserEmail(msg.AuthorId);
                }

                items.Add(CreateMessageItem(msg.CreateDt, author, msg.Body, false));
            }

            Items = items.OrderByDescending(i => i.SortTime).ToList();
        }

        public int CustomerId { get; }

        public string Name { get; }

        public string Email { get; }

        public bool IsRegistered { get; }

        public bool IsNewsletterSubscriber { get; }

        public bool IsDistributor { get; }

        public string TotalSpent { get; }

        public int? OmittedOrders { get; }

        public List<CustomerRelatedItem> Items { get; private set; }

        private static CustomerRelatedItem CreateMessageItem(DateTime dt, string author, string body, bool isCustomerMessage)
        {
            return CustomerRelatedItem.Create(dt, CustomerRelatedItem.MessageItemType, new MessageItem(author, body, isCustomerMessage));
        }

        private static IEnumerable<CustomerRelatedItem> CreateOrderItem(CustomerOverview cust, CustomerOrderOverview order, IProject project)
        {
            yield return
                CustomerRelatedItem.Create(
                    order.Dt,
                    CustomerRelatedItem.OrderItemType,
                    new OrderItem(order, cust.Currency));

            if (!string.IsNullOrWhiteSpace(order.CustomerMessage))
            {
                yield return CreateMessageItem(order.Dt.AddSeconds(-1), cust.Nick, order.CustomerMessage, true);
            }

            if (!string.IsNullOrWhiteSpace(order.InternalMessage))
            {
                yield return CreateMessageItem(order.Dt.AddSeconds(-2), project.Name, order.InternalMessage, false);
            }
        }

        #region nested

        internal class OrderItem
        {
            public OrderItem(CustomerOrderOverview order, string currency)
            {
                PurchaseOrderId = order.PurchaseOrderId;
                IsCanceled = order.IsCanceled;
                IsComplete = order.IsComplete;
                OrderNumber = order.OrderNumber;
                Total = $"{StringUtil.FormatDecimal(order.Total)} {currency}";
            }

            public long PurchaseOrderId { get; }

            public bool IsCanceled { get; }

            public bool IsComplete { get; }

            public string OrderNumber { get; }

            public string Total { get; }
        }

        internal class MessageItem
        {
            public MessageItem(string author, string body, bool isLeftByCustomer)
            {
                Author = author;
                Body = body;
                IsLeftByCustomer = isLeftByCustomer;
            }

            public string Author { get; }

            public string Body { get; }

            public bool IsLeftByCustomer { get; }
        }
        #endregion
    }
}
