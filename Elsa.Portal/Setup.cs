
using Elsa.App.Commerce.Payments;
using Elsa.App.Commerce.Preview;
using Elsa.Common;
using Elsa.Users;

using Robowire;
using Elsa.Commerce.Core;

namespace Elsa.Portal
{
    public static class Setup
    {
        public static void SetupContainer(IContainer container)
        {
            CommonRegistry.SetupContainer(container);

            container.Setup(s => s.For<IWebSession>().Use<UserWebSession>());
            container.Setup(s => s.For<ISession>().Import.FromFactory(l => l.Get<IWebSession>()));
            container.Setup(s => s.ScanAssembly(typeof(PreviewController).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(IPurchaseOrderRepository).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(PaymentsPairingController).Assembly));
        }
    }
}