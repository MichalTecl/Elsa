using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.SysCounters;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data.Adapters;
using Elsa.Invoicing.Core.Internal;
using Robowire;
using RobOrm.Core;

namespace Elsa.Invoicing.Core.Data
{
    public class InvoiceFormRepository : IInvoiceFormsRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ICache _cache;
        private readonly ISysCountersManager _countersManager;
        private readonly ILog _log;
        private readonly IServiceLocator _serviceLocator;
        private readonly IMaterialBatchFacade _batchFacade;
        
        public InvoiceFormRepository(IDatabase database, ISession session, ICache cache, ISysCountersManager countersManager, ILog log, IServiceLocator serviceLocator, IMaterialBatchFacade batchFacade)
        {
            _database = database;
            _session = session;
            _cache = cache;
            _countersManager = countersManager;
            _log = log;
            _serviceLocator = serviceLocator;
            _batchFacade = batchFacade;
        }

        public IEnumerable<IInvoiceFormType> GetInvoiceFormTypes()
        {
            return _cache.ReadThrough($"invoiceFormTypes_{_session.Project.Id}",
                TimeSpan.FromDays(1),
                () => _database.SelectFrom<IInvoiceFormType>().Where(it => it.ProjectId == _session.Project.Id)
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
                var formIds = _database.SelectFrom<IInvoiceFormItemMaterialBatch>()
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

            return query.Execute().Select(e => new InvoiceFormAdapter(_serviceLocator, e));
        }

        public IInvoiceForm GetTemplate(int typeId, Action<IInvoiceForm> setup)
        {
            return _database.New<IInvoiceForm>(f =>
            {
                f.ProjectId = _session.Project.Id;
                f.FormTypeId = typeId;
                setup(f);
            });
        }

        public IInvoiceFormItem GetItemTemplate()
        {
            return _database.New<IInvoiceFormItem>();
        }

        public IInvoiceForm SaveInvoiceForm(IInvoiceForm invoice, List<IInvoiceFormItem> items, List<KeyValuePair<IInvoiceFormItem, int>> itemBatchId)
        {
            int invoiceId = 0;

            using (var tx = _database.OpenTransaction())
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
                
                invoice.ProjectId = _session.Project.Id;

                if (string.IsNullOrWhiteSpace(invoice.InvoiceFormNumber))
                {
                    _countersManager.WithCounter(it.SystemCounterId.Value,
                        ifnr =>
                        {
                            invoice.InvoiceFormNumber = ifnr;
                            _database.Save(invoice);
                        });
                }
                else
                {
                    _database.Save(invoice);
                }

                invoiceId = invoice.Id;

                var existingItemBatches = _database.SelectFrom<IInvoiceFormItem>().Join(i => i.Batches)
                    .Where(i => i.InvoiceFormId == invoice.Id).Execute().ToList();

                foreach (var invoiceFormItem in items)
                {
                    invoiceFormItem.InvoiceFormId = invoice.Id;
                    _database.Save(invoiceFormItem);

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

                        var bridge = _database.New<IInvoiceFormItemMaterialBatch>(b =>
                        {
                            b.InvoiceFormItemId = invoiceFormItem.Id;
                            b.MaterialBatchId = batch.Value;
                        });
                        _database.Save(bridge);
                    }
                }
                
                tx.Commit();
            }

            return GetInvoiceFormById(invoiceId);
        }

        public IEnumerable<IInvoiceFormReportType> GetInvoiceFormReportTypes()
        {
            return _cache.ReadThrough($"invoiceReportTypes_{_session.Project.Id}",
                TimeSpan.FromDays(1),
                () => _database.SelectFrom<IInvoiceFormReportType>().OrderBy(r => r.ViewOrder).Execute().ToList());
        }
        
        public IInvoiceForm GetInvoiceFormById(int id)
        {
            return GetInvoiceQuery().Where(i => i.Id == id).Execute().Select(e => new InvoiceFormAdapter(_serviceLocator, e)).FirstOrDefault();
        }

        public IInvoiceFormGenerationLog AddEvent(int collectionId, string message, bool warning, bool error)
        {
            var entry = _database.New<IInvoiceFormGenerationLog>(e =>
            {
                e.InvoiceFormCollectionId = collectionId;
                e.EventDt = DateTime.Now;
                e.IsError = error;
                e.IsWarning = warning;
                e.Message = message;
            });

            _database.Save(entry);

            return entry;
        }

        public IInvoiceForm NewForm(Action<IInvoiceForm> setup)
        {
            var form = _database.New<IInvoiceForm>();
            form.ProjectId = _session.Project.Id;

            setup(form);

            _database.Save(form);

            return form;
        }

        public IInvoiceFormItem NewItem(IInvoiceForm form, int materialBatchId, Action<IInvoiceFormItem> setup)
        {
            using (var tx = _database.OpenTransaction())
            {
                var item = _database.New<IInvoiceFormItem>();
                item.InvoiceFormId = form.Id;

                setup(item);

                _database.Save(item);

                var bridge = _database.New<IInvoiceFormItemMaterialBatch>();
                bridge.MaterialBatchId = materialBatchId;
                bridge.InvoiceFormItemId = item.Id;

                _database.Save(bridge);

                tx.Commit();

                return item;
            }
        }

        public IInvoiceFormGenerationContext StartGeneration(string contextName, int year, int month, int invoiceformTypeId)
        {
            var collection = _database.New<IInvoiceFormCollection>();
            collection.GenerateUserId = _session.User.Id;
            collection.Name = contextName;
            collection.Year = year;
            collection.Month = month;
            collection.InvoiceFormTypeId = invoiceformTypeId;
            collection.ProjectId = _session.Project.Id;

            _database.Save(collection);

            var toDt = new DateTime(year, month, 1).AddDays(35);
            var fromDt = toDt.AddMonths(-3);

            return new InvoiceFormsGenerationContext(_log, this,  collection.Id, _batchFacade.CreatPriceBulkProvider(fromDt, toDt));
        }

        public IInvoiceFormCollection GetCollectionByMaterialBatchId(int batchId, int invoiceFormTypeId)
        {
            // todo: can be optimized
            return _database.SelectFrom<IInvoiceFormItemMaterialBatch>()
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
                .Execute().Select(e => new InvoiceFormCollectionAdapter(_serviceLocator, e))
                .FirstOrDefault();
        }

        public IInvoiceFormCollection FindCollection(int invoiceFormTypeId, int year, int month)
        {
            return GetCollectionsQuery().Where(c => c.InvoiceFormTypeId == invoiceFormTypeId)
                .Where(c => (c.Year == year) && (c.Month == month)).Execute().Select(e => new InvoiceFormCollectionAdapter(_serviceLocator, e)).FirstOrDefault();
        }
        
        public void DeleteCollection(int existingCollectionId)
        {
            using (var tx = _database.OpenTransaction())
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

                _database.Sql().Call("DeleteInvoiceFormCollection").WithParam("@collectionId", existingCollectionId)
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

            using (var tx = _database.OpenTransaction())
            {

                var warns = _database.SelectFrom<IInvoiceFormGenerationLog>()
                    .Join(l => l.InvoiceFormCollection)
                    .Where(l => l.InvoiceFormCollection.ProjectId == _session.Project.Id)
                    .Where(l => l.Id.InCsv(ids)).Execute().ToList();

                foreach (var item in warns)
                {
                    item.ApproveDt = DateTime.Now;
                    item.ApproveUserId = _session.User.Id;
                }

                _database.SaveAll(warns);

                tx.Commit();
            }
        }

        public void ApproveCollection(int id)
        {
            using (var tx = _database.OpenTransaction())
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

                foreach (var form in collection.Forms.OrderBy(f => f.IssueDate))
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
                    _countersManager.WithCounter(counterId, nv => form.InvoiceFormNumber = nv);
                    
                    _database.Save(form);
                }

                collection.ApproveDt = DateTime.Now;
                collection.ApproveUserId = _session.User.Id;
                
                _database.Save(collection);

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
            return _cache.ReadThrough($"rel_f_tasks_{_session.Project.Id}", TimeSpan.FromHours(1), () =>
            {
                return _database.SelectFrom<IReleasingFormsGenerationTask>()
                    .Join(t => t.Inventories)
                    .Where(t => t.ProjectId == _session.Project.Id).Execute().ToList();
            });
        }

        public IEnumerable<IInvoiceForm> GetInvoiceFormsByCollectionId(int collectionId)
        {
            return _database.SelectFrom<IInvoiceForm>()
                .Where(f => f.ProjectId == _session.Project.Id && f.InvoiceFormCollectionId == collectionId).Execute()
                .Select(e => new InvoiceFormAdapter(_serviceLocator, e));
        }

        public IEnumerable<IInvoiceFormGenerationLog> GetLogByCollectionId(int collectionId)
        {
            return _database.SelectFrom<IInvoiceFormGenerationLog>()
                .Where(l => l.InvoiceFormCollectionId == collectionId).Execute()
                .Select(i => new InvoiceFormGenerationLogAdapter(_serviceLocator, i));
        }

        public IEnumerable<IInvoiceFormItem> GetItemsByFormId(int invoiceFormId)
        {
            var form = GetInvoiceFormById(invoiceFormId);
            return GetItemsByCollection(form.InvoiceFormCollectionId).Where(i => i.InvoiceFormId == invoiceFormId);
        }

        public IEnumerable<IInvoiceFormItemMaterialBatch> GetFormItemBatchesByItemId(int invoiceFormItemId)
        {
            return _database.SelectFrom<IInvoiceFormItemMaterialBatch>()
                .Where(e => e.InvoiceFormItemId == invoiceFormItemId).Execute()
                .Select(e => new InvoiceFormMaterialBatchAdapter(_serviceLocator, e));
        }

        private IQueryBuilder<IInvoiceFormCollection> GetCollectionsQuery()
        {
            return _database.SelectFrom<IInvoiceFormCollection>()
                .Join(c => c.Forms)
                .Where(c => c.ProjectId == _session.Project.Id)
                .OrderBy(c => c.Forms.Each().IssueDate);
        }

        private IEnumerable<IInvoiceFormItem> GetItemsByCollection(int collectionId)
        {
            return _cache.ReadThrough($"invFrmItemsByCollid_{collectionId}", TimeSpan.FromMinutes(1), () => _database
                .SelectFrom<IInvoiceFormItem>().Join(i => i.InvoiceForm)
                .Where(i => i.InvoiceForm.InvoiceFormCollectionId == collectionId).Execute()
                .Select(e => new InvoiceFormItemAdapter(_serviceLocator, e)));
        }

        
        private IQueryBuilder<IInvoiceForm> GetInvoiceQuery()
        {
            var query = _database.SelectFrom<IInvoiceForm>()
                .OrderBy(i => i.IssueDate)
                .Where(i => i.ProjectId == _session.Project.Id);
            return query;
        }
    }
}
