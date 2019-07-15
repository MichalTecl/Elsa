
namespace Elsa.Invoicing.Core.Contract
{
    public interface IInvoiceFormsGenerationRunner
    {
        IInvoiceFormGenerationContext RunReceivingInvoicesGeneration(int invoiceFormTypeId, int year, int month);

        IInvoiceFormGenerationContext RunTasks(int year, int month);
    }
}
