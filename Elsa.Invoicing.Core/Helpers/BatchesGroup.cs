using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;

namespace Elsa.Invoicing.Core.Helpers
{
    public class BatchesGroup : IEnumerable<IMaterialBatch>
    {
        private readonly List<IMaterialBatch> m_batches = new List<IMaterialBatch>();

        private readonly List<BatchesGrouping.BatchClassifier> m_classifiers;

        internal BatchesGroup(List<BatchesGrouping.BatchClassifier> classifiers)
        {
            m_classifiers = classifiers;
        }

        public string InvoiceNumber
        {
            get
            {
                return m_batches.FirstOrDefault(b => !string.IsNullOrWhiteSpace(b.InvoiceNr))?.InvoiceNr ?? string.Empty;
            }
        }
        
        public IEnumerator<IMaterialBatch> GetEnumerator()
        {
            return m_batches.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal bool TryAdd(IMaterialBatch batch, IInvoiceFormGenerationContext context)
        {
            if (m_batches.Count == 0)
            {
                m_batches.Add(batch);
                return true;
            }

            var refBatch = m_batches.First();

            foreach (var classifier in m_classifiers)
            {
                if (classifier.Comparer(refBatch, batch))
                {
                    continue;
                }

                classifier.Context?.Invoke(refBatch, batch, context);

                return false;
            }

            m_batches.Add(batch);
            return true;
        }
    }
}
