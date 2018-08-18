using System;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public static class OrderStatus 
    {
        public static readonly IOrderStatus New = new Status(1, "New");
        public static readonly IOrderStatus PendingPayment = new Status(2, "PendingPayment");
        public static readonly IOrderStatus ReadyToPack = new Status(3, "ReadyToPack");
        public static readonly IOrderStatus Packed = new Status(4, "Packed");
        public static readonly IOrderStatus Sent = new Status(5, "Sent");
        public static readonly IOrderStatus Returned = new Status(6, "Returned");
        public static readonly IOrderStatus Canceled = new Status(7, "Canceled");
        public static readonly IOrderStatus Failed = new Status(8, "Failed");
        
        private sealed class Status : IOrderStatus
        {
            private readonly int m_id;
            private readonly string m_name;

            public Status(int id, string name)
            {
                m_name = name;
                m_id = id;
            }

            public int Id
            {
                get
                {
                    return m_id;
                }
                set
                {
                    throw new InvalidOperationException();
                }
            }

            public string Name
            {
                get
                {
                    return m_name;
                }
                set
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
