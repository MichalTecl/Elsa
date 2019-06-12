using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;

namespace Elsa.Invoicing.Core.Helpers
{
    public class BatchesGrouping
    {
        private readonly List<BatchClassifier> m_classifiers = new List<BatchClassifier>();

        private readonly List<Action<IMaterialBatch, IInvoiceFormGenerationContext>> m_validators = new List<Action<IMaterialBatch, IInvoiceFormGenerationContext>>();
        
        public void AddGrouping(Func<IMaterialBatch, IMaterialBatch, bool> compareReferenceWithNew,
            Action<IMaterialBatch, IMaterialBatch, IInvoiceFormGenerationContext> context)
        {
            m_classifiers.Add(new BatchClassifier(compareReferenceWithNew, context));
        }

        public void AddValidator(Action<IMaterialBatch, IInvoiceFormGenerationContext> validator)
        {
            m_validators.Add(validator);
        }

        public IEnumerable<BatchesGroup> GroupBatches(IEnumerable<IMaterialBatch> batches,
            IInvoiceFormGenerationContext log)
        {
            var groups = new List<BatchesGroup>();

            foreach (var batch in batches)
            {
                foreach (var validator in m_validators)
                {
                    validator(batch, log);
                }

                var placed = false;
                foreach (var group in groups)
                {
                    if (group.TryAdd(batch, log))
                    {
                        placed = true;
                        break;
                    }
                }

                if (placed)
                {
                    continue;
                }

                var newGroup = new BatchesGroup(m_classifiers);
                groups.Add(newGroup);

                if (!newGroup.TryAdd(batch, log))
                {
                    throw new InvalidOperationException("Batch wasn't grouped to new group");
                }
            }

            return groups;
        }
        
        internal class BatchClassifier
        {
            public readonly Func<IMaterialBatch, IMaterialBatch, bool> Comparer;

            public readonly Action<IMaterialBatch, IMaterialBatch, IInvoiceFormGenerationContext> Context;

            public BatchClassifier(Func<IMaterialBatch, IMaterialBatch, bool> comparer, Action<IMaterialBatch, IMaterialBatch, IInvoiceFormGenerationContext> context)
            {
                Comparer = comparer;
                Context = context;
            }
        }
    }
}
