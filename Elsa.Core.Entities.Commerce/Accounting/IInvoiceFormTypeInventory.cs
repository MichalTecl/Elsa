using Elsa.Core.Entities.Commerce.Inventory;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IInvoiceFormTypeInventory
    {
        int Id { get; }

        int InvoiceFormTypeId { get; set; }
        IInvoiceFormType InvoiceFormType { get; }

        int MaterialInventoryId { get; set; }
        IMaterialInventory MaterialInventory { get; }
    }
}
