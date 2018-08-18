using Elsa.Integration.PaymentSystems.Common.Internal;

using Robowire;

namespace Elsa.Integration.PaymentSystems.Common
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IPaymentSystemClientFactory>().Use<PaymentSystemClientFactory>();
        }
    }
}
