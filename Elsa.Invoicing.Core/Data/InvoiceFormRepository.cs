using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.SysCounters;
using Elsa.Core.Entities.Commerce.Accounting;

using Robowire.RobOrm.Core;

namespace Elsa.Invoicing.Core.Data
{
    public class InvoiceFormRepository : IInvoiceFormsRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly ICache m_cache;
        private readonly ISysCountersManager m_countersManager;
        
        public InvoiceFormRepository(IDatabase database, ISession session, ICache cache, ISysCountersManager countersManager)
        {
            m_database = database;
            m_session = session;
            m_cache = cache;
            m_countersManager = countersManager;
        }

        public IEnumerable<IInvoiceFormType> GetInvoiceFormTypes()
        {
            return m_cache.ReadThrough($"invoiceFormTypes_{m_session.Project.Id}",
                TimeSpan.FromDays(1),
                () => m_database.SelectFrom<IInvoiceFormType>().Where(it => it.ProjectId == m_session.Project.Id)
                    .Execute().ToList());
        }

        public IInvoiceFormType GetInvoiceFormType(int id)
        {
            return GetInvoiceFormTypes().FirstOrDefault(i => i.Id == id);
        }

        public IEnumerable<IInvoiceFormTypeInventory> GetInvoiceFormTypeInventories()
        {
            return m_cache.ReadThrough($"invoiceFormTypeInventories_{m_session.Project.Id}",
                TimeSpan.FromDays(1),
                () => m_database.SelectFrom<IInvoiceFormTypeInventory>()
                    .Join(i => i.InvoiceFormType)
                    .Join(i => i.MaterialInventory)
                    .Where(i => i.InvoiceFormType.ProjectId == m_session.Project.Id)
                    .Where(i => i.MaterialInventory.ProjectId == m_session.Project.Id)
                    .Execute()
                    .ToList());
        }

        public IEnumerable<IInvoiceForm> FindInvoiceForms(int? invoiceFormTypeId,
            int? materialBatchId,
            string externalInvoiceNumber,
            int? supplierId)
        {
            var query = GetInvoiceQuery();

            if (invoiceFormTypeId != null)
            {
                query = query.Where(i => i.FormTypeId == invoiceFormTypeId.Value);
            }

            if (materialBatchId != null)
            {
                var formIds = m_database.SelectFrom<IInvoiceFormItemMaterialBatch>()
                    .Where(i => i.MaterialBatchId == materialBatchId.Value).Join(i => i.InvoiceFormItem).Execute()
                    .Select(i => i.InvoiceFormItem.InvoiceFormId).Distinct().ToList();

                if (formIds.Count == 0)
                {
                    return new IInvoiceForm[0];
                }

                query = query.Where(i => i.Id.InCsv(formIds));
            }

            if (!string.IsNullOrWhiteSpace(externalInvoiceNumber))
            {
                query = query.Where(i => i.InvoiceNumber == externalInvoiceNumber);
            }

            if (supplierId != null)
            {
                query = query.Where(i => i.SupplierId == supplierId.Value);
            }

            return query.Execute();
        }

        public IInvoiceForm GetTemplate(int typeId, Action<IInvoiceForm> setup)
        {
            return m_database.New<IInvoiceForm>(f =>
            {
                f.ProjectId = m_session.Project.Id;
                f.FormTypeId = typeId;
                setup(f);
            });
        }

        public IInvoiceFormItem GetItemTemplate()
        {
            return m_database.New<IInvoiceFormItem>();
        }

        public IInvoiceForm SaveInvoiceForm(IInvoiceForm invoice, List<IInvoiceFormItem> items, List<KeyValuePair<IInvoiceFormItem, int>> itemBatchId)
        {
            int invoiceId = 0;

            using (var tx = m_database.OpenTransaction())
            {
                var it = GetInvoiceFormType(invoice.FormTypeId);
                if (it == null)
                {
                    throw new InvalidOperationException("Cannot save InvoiceForm without type");
                }

                if (it.SystemCounterId == null)
                {
                    throw new InvalidOperationException("Cannot save InvoiceForm of type without reference to counter");
                }
                
                invoice.ProjectId = m_session.Project.Id;

                if (string.IsNullOrWhiteSpace(invoice.InvoiceFormNumber))
                {
                    m_countersManager.WithCounter(it.SystemCounterId.Value,
                        ifnr =>
                        {
                            invoice.InvoiceFormNumber = ifnr;
                            m_database.Save(invoice);
                        });
                }
                else
                {
                    m_database.Save(invoice);
                }

                invoiceId = invoice.Id;

                var existingItemBatches = m_database.SelectFrom<IInvoiceFormItem>().Join(i => i.Batches)
                    .Where(i => i.InvoiceFormId == invoice.Id).Execute().ToList();

                foreach (var invoiceFormItem in items)
                {
                    invoiceFormItem.InvoiceFormId = invoice.Id;
                    m_database.Save(invoiceFormItem);

                    var batches = itemBatchId.Where(ib => ib.Key == invoiceFormItem).ToList();

                    foreach (var batch in batches)
                    {
                        var alreadyExists = existingItemBatches.Any(itm =>
                            (itm.Id == invoiceFormItem.Id) &&
                            itm.Batches.Any(itb => itb.MaterialBatchId == batch.Value));

                        if (alreadyExists)
                        {
                            continue;
                        }

                        var bridge = m_database.New<IInvoiceFormItemMaterialBatch>(b =>
                        {
                            b.InvoiceFormItemId = invoiceFormItem.Id;
                            b.MaterialBatchId = batch.Value;
                        });
                        m_database.Save(bridge);
                    }
                }
                
                tx.Commit();
            }

            return GetInvoiceForm(invoiceId);
        }

        public IInvoiceForm GetInvoiceForm(int id)
        {
            return GetInvoiceQuery().Where(i => i.Id == id).Take(1).Execute().FirstOrDefault();
        }

        private IQueryBuilder<IInvoiceForm> GetInvoiceQuery()
        {
            var query = m_database.SelectFrom<IInvoiceForm>()
                .Join(i => i.Items)
                .Join(i => i.Items.Each().Batches)
                .Join(i => i.Items.Each().Conversion)
                .Join(i => i.Items.Each().SourceCurrency)
                .Join(i => i.Items.Each().Unit)
                .Join(i => i.FormType)
                .Join(i => i.Supplier)
                .Where(i => i.ProjectId == m_session.Project.Id);
            return query;
        }
    }
}
