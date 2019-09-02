using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common.Data;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire;

namespace Elsa.Invoicing.Core.Data.Adapters
{
    internal class InvoiceFormMaterialBatchAdapter : AdapterBase<IInvoiceFormItemMaterialBatch>, IInvoiceFormItemMaterialBatch
    {
        public InvoiceFormMaterialBatchAdapter(IServiceLocator serviceLocator, IInvoiceFormItemMaterialBatch adaptee) : base(serviceLocator, adaptee)
        {
        }

        public int Id => Adaptee.Id;
        public IInvoiceFormItem InvoiceFormItem { get; }
        public int InvoiceFormItemId { get; set; }
        public int MaterialBatchId { get => Adaptee.MaterialBatchId; set => Adaptee.MaterialBatchId = value; }

        public IMaterialBatch MaterialBatch =>
            Get<IMaterialBatchRepository, IMaterialBatch>("MaterialBatch", r => r.GetBatchById(MaterialBatchId)?.Batch);
    }
}
