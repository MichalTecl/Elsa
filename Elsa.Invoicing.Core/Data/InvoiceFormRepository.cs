using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Logging;
using Elsa.Common.SysCounters;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data.Adapters;
using Elsa.Invoicing.Core.Internal;
using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Invoicing.Core.Data
{
    public class InvoiceFormRepository : IInvoiceFormsRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly ICache m_cache;
        private readonly ISysCountersManager m_countersManager;
        private readonly ILog m_log;
        private readonly IServiceLocator m_serviceLocator;
        
        public InvoiceFormRepository(IDatabase database, ISession session, ICache cache, ISysCountersManager countersManager, ILog log, IServiceLocator serviceLocator)
        {
            m_database = database;
            m_session = session;
            m_cache = cache;
            m_countersManager = countersManager;
            m_log = log;
            m_serviceLocator = serviceLocator;
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
        
        public IEnumerable<IInvoiceForm> FindInvoiceForms(int? invoiceFormTypeId,
            int? materialBatchId,
            string externalInvoiceNumber,
            int? supplierId, 
            DateTime? from, 
            DateTime? to)
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

            if (from != null)
            {
                query = query.Where(i => i.IssueDate >= from.Value);
            }

            if (to != null)
            {
                query = query.Where(i => i.IssueDate <= to.Value);
            }

            return query.Execute().Select(e => new InvoiceFormAdapter(m_serviceLocator, e));
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

            return GetInvoiceFormById(invoiceId);
        }

        public IEnumerable<IInvoiceFormReportType> GetInvoiceFormReportTypes()
        {
            return m_cache.ReadThrough($"invoiceReportTypes_{m_session.Project.Id}",
                TimeSpan.FromDays(1),
                () => m_database.SelectFrom<IInvoiceFormReportType>().OrderBy(r => r.ViewOrder).Execute().ToList());
        }
        
        public IInvoiceForm GetInvoiceFormById(int id)
        {
            return GetInvoiceQuery().Where(i => i.Id == id).Execute().Select(e => new InvoiceFormAdapter(m_serviceLocator, e)).FirstOrDefault();
        }

        public IInvoiceFormGenerationLog AddEvent(int collectionId, string message, bool warning, bool error)
        {
            var entry = m_database.New<IInvoiceFormGenerationLog>(e =>
            {
                e.InvoiceFormCollectionId = collectionId;
                e.EventDt = DateTime.Now;
                e.IsError = error;
                e.IsWarning = warning;
                e.Message = message;
            });

            m_database.Save(entry);

            return entry;
        }

        public IInvoiceForm NewForm(Action<IInvoiceForm> setup)
        {
            var form = m_database.New<IInvoiceForm>();
            form.ProjectId = m_session.Project.Id;

            setup(form);

            m_database.Save(form);

            return form;
        }

        public IInvoiceFormItem NewItem(IInvoiceForm form, int materialBatchId, Action<IInvoiceFormItem> setup)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var item = m_database.New<IInvoiceFormItem>();
                item.InvoiceFormId = form.Id;

                setup(item);

                m_database.Save(item);

                var bridge = m_database.New<IInvoiceFormItemMaterialBatch>();
                bridge.MaterialBatchId = materialBatchId;
                bridge.InvoiceFormItemId = item.Id;

                m_database.Save(bridge);

                tx.Commit();

                return item;
            }
        }

        public IInvoiceFormGenerationContext StartGeneration(string contextName, int year, int month, int invoiceformTypeId)
        {
            var collection = m_database.New<IInvoiceFormCollection>();
            collection.GenerateUserId = m_session.User.Id;
            collection.Name = contextName;
            collection.Year = year;
            collection.Month = month;
            collection.InvoiceFormTypeId = invoiceformTypeId;
            collection.ProjectId = m_session.Project.Id;

            m_database.Save(collection);

            return new InvoiceFormsGenerationContext(m_log, this,  collection.Id);
        }

        public IInvoiceFormCollection GetCollectionByMaterialBatchId(int batchId, int invoiceFormTypeId)
        {
            // todo: can be optimized
            return m_database.SelectFrom<IInvoiceFormItemMaterialBatch>()
                .Join(b => b.InvoiceFormItem)
                .Join(b => b.InvoiceFormItem.InvoiceForm)
                .Join(b => b.InvoiceFormItem.InvoiceForm.InvoiceFormCollection)
                .Where(b => b.MaterialBatchId == batchId)
                .Where(b => b.InvoiceFormItem.InvoiceForm.FormTypeId == invoiceFormTypeId)
                .Execute().FirstOrDefault()?.InvoiceFormItem?.InvoiceForm?.InvoiceFormCollection;
        }

        public IInvoiceFormCollection GetCollectionById(int collectionId)
        {
            return GetCollectionsQuery()
                .Where(c => c.Id == collectionId)
                .Execute().Select(e => new InvoiceFormCollectionAdapter(m_serviceLocator, e))
                .FirstOrDefault();
        }

        public IInvoiceFormCollection FindCollection(int invoiceFormTypeId, int year, int month)
        {
            return GetCollectionsQuery().Where(c => c.InvoiceFormTypeId == invoiceFormTypeId)
                .Where(c => (c.Year == year) && (c.Month == month)).Execute().Select(e => new InvoiceFormCollectionAdapter(m_serviceLocator, e)).FirstOrDefault();
        }
        
        public void DeleteCollection(int existingCollectionId)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var collection = GetCollectionById(existingCollectionId);
                if (collection == null)
                {
                    throw new InvalidOperationException("Invalid entity reference");
                }

                if ((collection.ApproveDt != null) || (collection.ApproveUserId != null))
                {
                    throw new InvalidOperationException("Soupiska jiz byla schvalena");
                }

                m_database.Sql().Call("DeleteInvoiceFormCollection").WithParam("@collectionId", existingCollectionId)
                    .NonQuery();
                
                tx.Commit();
            }
        }

        public void ApproveLogWarnings(List<int> ids)
        {
            if (ids == null)
            {
                throw new InvalidOperationException("No ids received");
            }

            using (var tx = m_database.OpenTransaction())
            {

                var warns = m_database.SelectFrom<IInvoiceFormGenerationLog>()
                    .Join(l => l.InvoiceFormCollection)
                    .Where(l => l.InvoiceFormCollection.ProjectId == m_session.Project.Id)
                    .Where(l => l.Id.InCsv(ids)).Execute().ToList();

                foreach (var item in warns)
                {
                    item.ApproveDt = DateTime.Now;
                    item.ApproveUserId = m_session.User.Id;
                }

                m_database.SaveAll(warns);

                tx.Commit();
            }
        }

        public void ApproveCollection(int id)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var collection = GetCollectionById(id);
                if (collection == null)
                {
                    throw new InvalidOperationException("Invalid entity reference");
                }

                if ((collection.ApproveDt != null) || (collection.ApproveUserId != null))
                {
                    throw new InvalidOperationException("Soupiska jiz byla schvalena");
                }

                if ((collection.Log.Any(e => e.IsError || (e.IsWarning && (e.ApproveUserId == null)))))
                {
                    throw new InvalidOperationException("Soupisku nelze schvalit, protoze ma neschvalena varovani nebo chyby");
                }

                foreach (var form in collection.Forms)
                {
                    int counterId;

                    if (form.CounterId != null)
                    {
                        counterId = form.CounterId.Value;
                    }
                    else if (form.SourceTaskId != null)
                    {
                        var task = GetTask(form.SourceTaskId.Value);
                        counterId = task.CounterId;
                    }
                    else if (form.FormType?.SystemCounterId != null)
                    {
                        counterId = form.FormType.SystemCounterId.Value;
                    }
                    else
                    {
                        throw new InvalidOperationException("SystemCounter not set");
                    }

                    //TODO - this is wrong. All used counters should be in exclusive use to generate continuous sequnece of numbers
                    m_countersManager.WithCounter(counterId, nv => form.InvoiceFormNumber = nv);
                    
                    m_database.Save(form);
                }

                collection.ApproveDt = DateTime.Now;
                collection.ApproveUserId = m_session.User.Id;
                
                m_database.Save(collection);

                tx.Commit();
            }
        }

        private IReleasingFormsGenerationTask GetTask(int id)
        {
            var task = GetReleasingFormsTasks().FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            return task;
        }

        public IEnumerable<IReleasingFormsGenerationTask> GetReleasingFormsTasks()
        {
            return m_cache.ReadThrough($"rel_f_tasks_{m_session.Project.Id}", TimeSpan.FromHours(1), () =>
            {
                return m_database.SelectFrom<IReleasingFormsGenerationTask>()
                    .Join(t => t.Inventories)
                    .Where(t => t.ProjectId == m_session.Project.Id).Execute().ToList();
            });
        }

        public IEnumerable<IInvoiceForm> GetInvoiceFormsByCollectionId(int collectionId)
        {
            return m_database.SelectFrom<IInvoiceForm>()
                .Where(f => f.ProjectId == m_session.Project.Id && f.InvoiceFormCollectionId == collectionId).Execute()
                .Select(e => new InvoiceFormAdapter(m_serviceLocator, e));
        }

        public IEnumerable<IInvoiceFormGenerationLog> GetLogByCollectionId(int collectionId)
        {
            return m_database.SelectFrom<IInvoiceFormGenerationLog>()
                .Where(l => l.InvoiceFormCollectionId == collectionId).Execute()
                .Select(i => new InvoiceFormGenerationLogAdapter(m_serviceLocator, i));
        }

        public IEnumerable<IInvoiceFormItem> GetItemsByFormId(int invoiceFormId)
        {
            var form = GetInvoiceFormById(invoiceFormId);
            return GetItemsByCollection(form.InvoiceFormCollectionId).Where(i => i.InvoiceFormId == invoiceFormId);
        }

        public IEnumerable<IInvoiceFormItemMaterialBatch> GetFormItemBatchesByItemId(int invoiceFormItemId)
        {
            return m_database.SelectFrom<IInvoiceFormItemMaterialBatch>()
                .Where(e => e.InvoiceFormItemId == invoiceFormItemId).Execute()
                .Select(e => new InvoiceFormMaterialBatchAdapter(m_serviceLocator, e));
        }

        private IQueryBuilder<IInvoiceFormCollection> GetCollectionsQuery()
        {
            return m_database.SelectFrom<IInvoiceFormCollection>()
                .Join(c => c.Forms)
                .Where(c => c.ProjectId == m_session.Project.Id)
                .OrderBy(c => c.Forms.Each().IssueDate);
        }

        private IEnumerable<IInvoiceFormItem> GetItemsByCollection(int collectionId)
        {
            return m_cache.ReadThrough($"invFrmItemsByCollid_{collectionId}", TimeSpan.FromMinutes(1), () => m_database
                .SelectFrom<IInvoiceFormItem>().Join(i => i.InvoiceForm)
                .Where(i => i.InvoiceForm.InvoiceFormCollectionId == collectionId).Execute()
                .Select(e => new InvoiceFormItemAdapter(m_serviceLocator, e)));
        }

        
        private IQueryBuilder<IInvoiceForm> GetInvoiceQuery()
        {
            var query = m_database.SelectFrom<IInvoiceForm>()
                .OrderBy(i => i.IssueDate)
                .Where(i => i.ProjectId == m_session.Project.Id);
            return query;
        }
    }
}
