
namespace Elsa.Invoicing.Core.Contract
{
    public interface IInvoiceFormsGenerationRunner
    {
        IInvoiceFormGenerationContext Run(int invoiceFormTypeId, int year, int month);
    }
}
