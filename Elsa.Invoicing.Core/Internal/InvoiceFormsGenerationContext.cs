using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Invoicing.Core.Internal
{
    internal class InvoiceFormsGenerationContext : IInvoiceFormGenerationContext
    {
        private readonly ILog m_log;
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;
        private readonly int m_collectionId;
        private HashSet<string> m_preapprovedWarnings = new HashSet<string>();
        private readonly IBatchPriceBulkProvider m_batchPriceBulkProvider;
        
        public InvoiceFormsGenerationContext(ILog log, IInvoiceFormsRepository invoiceFormsRepository, int collectionId, IBatchPriceBulkProvider batchPriceBulkProvider)
        {
            m_log = log;
            m_invoiceFormsRepository = invoiceFormsRepository;
            m_collectionId = collectionId;
            m_batchPriceBulkProvider = batchPriceBulkProvider;
        }

        public bool HasErrors { get; private set; }

        public void Info(string inf,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            m_log.Info(inf, member, path, line);

            m_invoiceFormsRepository.AddEvent(m_collectionId, inf, false, false);
        }

        public void Warning(string inf,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            m_log.Info($"WARN: {inf}", member, path, line);
            
            var evt = m_invoiceFormsRepository.AddEvent(m_collectionId, inf, true, false);

            if (m_preapprovedWarnings.Contains(inf))
            {
                m_invoiceFormsRepository.ApproveLogWarnings(new List<int>{ evt.Id });
            }
        }

        public void Error(string message,
            Exception ex = null,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            HasErrors = true;

            if (ex == null)
            {
                m_log.Error(message, member, path, line);
            }
            else
            {
                m_log.Error(message, ex, member, path, line);
            }

            m_invoiceFormsRepository.AddEvent(m_collectionId, message, false, true);
        }

        public IInvoiceForm NewInvoiceForm(Action<IInvoiceForm> setup)
        {
            return m_invoiceFormsRepository.NewForm(f =>
            {
                f.InvoiceFormCollectionId = m_collectionId;
                setup(f);
            });
        }

        public IInvoiceFormItem NewFormItem(IInvoiceForm form, IMaterialBatch batch, Action<IInvoiceFormItem> setup)
        {
            return m_invoiceFormsRepository.NewItem(form, batch.Id, setup);
        }

        public void AutoApproveWarnings(HashSet<string> preapprovedMessages)
        {
            m_preapprovedWarnings = preapprovedMessages;
        }

        public int CountForms()
        {
            var coll = m_invoiceFormsRepository.GetCollectionById(m_collectionId);

            return coll?.Forms.Count() ?? 0;
        }

        public List<PriceComponentModel> GetBatchPriceComponents(int batchId)
        {
            return m_batchPriceBulkProvider.GetBatchPriceComponents(batchId);
        }
    }
}
