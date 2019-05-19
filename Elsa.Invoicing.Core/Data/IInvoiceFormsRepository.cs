using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Accounting;

namespace Elsa.Invoicing.Core.Data
{
    public interface IInvoiceFormsRepository
    {
        IEnumerable<IInvoiceFormType> GetInvoiceFormTypes();

        IInvoiceFormType GetInvoiceFormType(int id);

        IEnumerable<IInvoiceFormTypeInventory> GetInvoiceFormTypeInventories();

        IEnumerable<IInvoiceForm> FindInvoiceForms(int? invoiceFormTypeId, int? materialBatchId, string externalInvoiceNumber, int? supplierId, DateTime? from, DateTime? to);

        IInvoiceForm GetTemplate(int typeId,  Action<IInvoiceForm> setup);

        IInvoiceFormItem GetItemTemplate();

        IInvoiceForm SaveInvoiceForm(IInvoiceForm invoice, List<IInvoiceFormItem> items, List<KeyValuePair<IInvoiceFormItem, int>> itemBatchId);

        IEnumerable<IInvoiceFormReportType> GetInvoiceFormReportTypes();
    }
}
