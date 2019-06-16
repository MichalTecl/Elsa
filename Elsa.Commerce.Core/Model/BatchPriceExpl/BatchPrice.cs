using System.Collections.Generic;
using System.Linq;

using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Newtonsoft.Json;

namespace Elsa.Commerce.Core.Model.BatchPriceExpl
{
    public class BatchPrice
    {
        private readonly List<BatchPriceComponent> m_components = new List<BatchPriceComponent>();

        public BatchPrice(IMaterialBatch batch)
        {
            Batch = batch;
        }

        [JsonIgnore]
        public IMaterialBatch Batch { get; }

        public bool HasWarning => m_components.Any(c => c.IsWarning);

        public IEnumerable<BatchPriceComponent> Components => m_components;

        public decimal TotalPriceInPrimaryCurrency => Components.Where(c => c.Price != null).Sum(c => c.Price) ?? 0;

        public BatchPriceComponent AddComponent(bool isWarning, int? sourceBatchId, string text, decimal? price)
        {
            var component = new BatchPriceComponent()
            {
                Price = price,
                IsWarning = isWarning,
                SourceBatchId = sourceBatchId,
                Text = text
            };

            m_components.Add(component);

            return component;
        }
    }

    public class BatchPriceComponent
    {
        private bool m_isWarning = false;

        public bool IsWarning
        {
            get =>  m_isWarning = (m_isWarning || ChildPrices.Any(p => p.HasWarning));
            set => m_isWarning = value;
        }

        public int? SourceBatchId { get; set; }

        public string Text { get; set; }

        public decimal? Price { get; set; }

        public List<BatchPrice> ChildPrices => new List<BatchPrice>();
    }
}
