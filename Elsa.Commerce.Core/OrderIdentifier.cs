namespace Elsa.Commerce.Core
{
    public sealed class OrderIdentifier
    {
        public OrderIdentifier(string erpOrderId, string orderHash)
        {
            ErpOrderId = erpOrderId;
            OrderHash = orderHash;
        }

        public string ErpOrderId { get; }

        public string OrderHash { get; }
    }
}
