using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Crm;

namespace Elsa.Commerce.Core.Crm.Model
{
    public class CustomerOverview : ICommonCustomerInfo
    {
        public int CustomerId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public bool IsRegistered { get; set; }

        public bool IsNewsletterSubscriber { get; set; }

        public bool IsDistributor { get; set; }

        public decimal TotalSpent { get; set; }

        public string Currency { get; set; }

        public string Nick { get; set; }

        public List<CustomerOrderOverview> Orders { get; } = new List<CustomerOrderOverview>();

        public List<ICustomerRelatedNote> Messages { get; } = new List<ICustomerRelatedNote>();
    }

    public class CustomerOrderOverview
    {
        public long PurchaseOrderId { get; set; }

        public bool IsCanceled { get; set; }

        public bool IsComplete { get; set; }

        public string OrderNumber { get; set; }

        public decimal Total { get; set; }

        public DateTime Dt { get; set; }

        public string CustomerMessage { get; set; }

        public string InternalMessage { get; set; }
    }
    
}
