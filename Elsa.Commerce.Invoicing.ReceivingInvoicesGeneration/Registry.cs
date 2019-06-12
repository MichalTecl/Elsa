using Elsa.Invoicing.Core.Contract;

using Robowire;

namespace Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IInvoiceFormGeneratorFactory>().Use<InvoiceFormGeneratorFactory>();
            setup.For<IInvoiceFormsGenerationRunner>().Use<InvoiceFormsGenerationRunner>();
        }
    }
}
