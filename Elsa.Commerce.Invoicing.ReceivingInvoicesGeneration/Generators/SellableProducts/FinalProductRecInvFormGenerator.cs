using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.PremanufacturedMixtures;
using Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.PurchasedMaterial;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.SellableProducts
{
    internal class FinalProductRecInvFormGenerator : PremanufacturedMixturesInvFrmGenerator
    {
        public FinalProductRecInvFormGenerator(IMaterialBatchFacade batchFacade, IInvoiceFormsRepository invoiceFormsRepository, IMaterialRepository materialRepository)
            : base(batchFacade, invoiceFormsRepository, materialRepository)
        {
        }
    }
}
