using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Integration.PaymentSystems.Common
{
    public interface IPaymentSystemClient : IPaymentSystemHub
    {
        IPaymentSource Entity { get; set; }

        IPaymentSystemCommonSettings CommonSettings { get; }
    }
}
