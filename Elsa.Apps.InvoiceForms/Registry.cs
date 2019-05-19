using Elsa.Apps.InvoiceForms.Facade;

using Robowire;

namespace Elsa.Apps.InvoiceForms
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<InvoiceFormsQueryingFacade>().Use<InvoiceFormsQueryingFacade>();
        }
    }
}
