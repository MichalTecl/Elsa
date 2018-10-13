namespace Elsa.Commerce.Core.Crm.Model
{
    public interface ICommonCustomerInfo
    {
        int CustomerId { get; }

        string Name { get; }

        string Email { get; }

        bool IsRegistered { get; }

        bool IsNewsletterSubscriber { get; }

        bool IsDistributor { get; }
    }
}
