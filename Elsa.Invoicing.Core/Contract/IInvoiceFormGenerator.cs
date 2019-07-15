using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Invoicing.Core.Contract
{
    public interface IInvoiceFormGenerator
    {
        string GetGenerationName(IMaterialInventory forInventory, int year, int month);

        void Generate(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context, IReleasingFormsGenerationTask task = null);
    }
}
