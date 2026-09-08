using Elsa.Jobs.OrdersPostprocessing.Steps;
using Robowire;

namespace Elsa.Jobs.OrdersPostprocessing
{
    public class OrdersPostprocessingRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<SendPaymentReminder>().Use<SendPaymentReminder>();
        }
    }
}
