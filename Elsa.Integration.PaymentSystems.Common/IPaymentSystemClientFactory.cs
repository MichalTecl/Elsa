using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Integration.PaymentSystems.Common
{
    public interface IPaymentSystemClientFactory
    {
        IPaymentSystemClient GetClient(IPaymentSource source);

        IPaymentSystemHub GetPaymentSystems();

        IEnumerable<IPaymentSystemClient> GetAllPaymentSystemClients();
    }
}
