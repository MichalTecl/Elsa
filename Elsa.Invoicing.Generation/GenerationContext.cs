using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Invoicing.Generation
{
    public class GenerationContext
    {
        private readonly List<IInvoiceFormItem> m_items = new List<IInvoiceFormItem>();
        private readonly List<KeyValuePair<IInvoiceFormItem, int>> m_itemBatchId = new List<KeyValuePair<IInvoiceFormItem, int>>();

        private IInvoiceForm m_invoice = null;

        public IMaterialBatch SourceBatch { get; set; }
        
        public int InvoiceFormTypeId { get; set; }

        public IInvoiceForm InvoiceForm
        {
            get => m_invoice;
            set
            {
                if (m_invoice != null)
                {
                    throw new NotSupportedException("Cannot reassign the InvoiceForm to the existing context");
                }

                m_invoice = value;

                if (m_invoice?.Items != null)
                {
                    m_items.AddRange(m_invoice.Items);
                }
            }
        }

        public IInvoiceFormItem GetOrAddItem(IInvoiceFormsRepository invoiceFormsRepository, int batchId)
        {
            var item = m_items.Where(i => i.Batches.Any(b => b.MaterialBatchId == batchId))
                .OrderByDescending(i => i.ItemLogicalNumber).FirstOrDefault();

            if (item != null)
            {
                return item;
            }

            item = invoiceFormsRepository.GetItemTemplate();
            m_items.Add(item);
            m_itemBatchId.Add(new KeyValuePair<IInvoiceFormItem, int>(item, batchId));

            return item;
        }

        public void SaveInvoice(IInvoiceFormsRepository invoiceFormsRepository)
        {
            invoiceFormsRepository.SaveInvoiceForm(m_invoice, m_items, m_itemBatchId);
        }
    }
}
