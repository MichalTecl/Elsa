using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Invoicing.Core.Contract;

namespace Elsa.Invoicing.Core.Data
{
    public interface IInvoiceFormsRepository
    {
        IEnumerable<IInvoiceFormType> GetInvoiceFormTypes();

        IInvoiceFormType GetInvoiceFormType(int id);

        IEnumerable<IInvoiceForm> FindInvoiceForms(int? invoiceFormTypeId, int? materialBatchId, string externalInvoiceNumber, int? supplierId, DateTime? from, DateTime? to);

        IInvoiceForm GetTemplate(int typeId,  Action<IInvoiceForm> setup);

        IInvoiceFormItem GetItemTemplate();

        IInvoiceForm SaveInvoiceForm(IInvoiceForm invoice, List<IInvoiceFormItem> items, List<KeyValuePair<IInvoiceFormItem, int>> itemBatchId);

        IEnumerable<IInvoiceFormReportType> GetInvoiceFormReportTypes();

        IInvoiceForm GetInvoiceFormById(int id);

        IInvoiceFormGenerationLog AddEvent(int collectionId, string message, bool warning, bool error);

        IInvoiceForm NewForm(Action<IInvoiceForm> setup);

        IInvoiceFormItem NewItem(IInvoiceForm form, int materialBatchId, Action<IInvoiceFormItem> setup);

        IInvoiceFormGenerationContext StartGeneration(string contextName, int year, int month, int invoiceformTypeId);

        IInvoiceFormCollection GetCollectionByMaterialBatchId(int batchId);

        IInvoiceFormCollection GetCollectionById(int collectionId);

        IInvoiceFormCollection FindCollection(int invoiceFormTypeId, int year, int month);

        void DeleteCollection(int existingCollectionId);

        void ApproveLogWarnings(List<int> ids);
    }
}
