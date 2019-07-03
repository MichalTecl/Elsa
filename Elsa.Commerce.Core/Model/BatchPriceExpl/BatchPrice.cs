using System.Collections.Generic;
using System.Linq;
using System.Text;

using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Newtonsoft.Json;

namespace Elsa.Commerce.Core.Model.BatchPriceExpl
{
    public class BatchPrice : IBatchPrice
    {
        private readonly List<BatchPriceComponent> m_components = new List<BatchPriceComponent>();

        public BatchPrice(IMaterialBatch batch)
        {
            Batch = batch;
            BatchNumber = batch.BatchNumber;
        }

        [JsonIgnore]
        public IMaterialBatch Batch { get; }

        public string BatchNumber { get; }
        
        public bool HasWarning => m_components.Any(c => c.IsWarning);

        public IEnumerable<BatchPriceComponent> Components => m_components;

        public decimal TotalPriceInPrimaryCurrency => Components.Where(c => c.Price != null).Sum(c => c.Price) ?? 0;

        public string Text
        {
            get
            {
                var sb = new StringBuilder();
                RenderText(sb, 0);
                return sb.ToString();
            }
        }

        public void RenderText(StringBuilder sb, int level = 0)
        {
            var spacer = string.Empty.PadLeft(level, '\t');

            sb.Append(spacer)
              .Append(HasWarning ? "NEKOMPLETNÍ c" : "C")
             .AppendLine($"ena šarže {BatchNumber} = {TotalPriceInPrimaryCurrency.Display("CZK")}:");

            foreach (var comp in Components)
            {
                sb.Append("\t").Append(spacer).AppendLine(comp.Text);

                foreach (var compChildPrice in comp.ChildPrices)
                {
                    compChildPrice.RenderText(sb, level + 2);
                }
            }
        }

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

        public static IBatchPrice Combine(IEnumerable<BatchPrice> prices)
        {
            return new MultiBatchPrice(prices);
        }
    }

    public class BatchPriceComponent
    {
        private bool m_isWarning;

        public bool IsWarning
        {
            get =>  m_isWarning = (m_isWarning || ChildPrices.Any(p => p.HasWarning));
            set => m_isWarning = value;
        }

        public int? SourceBatchId { get; set; }

        public string Text { get; set; }

        public decimal? Price { get; set; }

        public List<BatchPrice> ChildPrices { get; } = new List<BatchPrice>();
    }

    internal sealed class MultiBatchPrice : IBatchPrice
    {
        private readonly List<BatchPriceComponent> m_components;

        public MultiBatchPrice(IEnumerable<BatchPrice> prices)
        {
            m_components = new List<BatchPriceComponent>();

            foreach (var p in prices)
            {
                var c = new BatchPriceComponent
                {
                    IsWarning = p.HasWarning,
                    Price = p.TotalPriceInPrimaryCurrency
                };
                c.ChildPrices.Add(p);
                c.SourceBatchId = p.Batch?.Id ?? -1;

                m_components.Add(c);
            }
        }

        public bool HasWarning => Components.Any(c => c.IsWarning);

        public IEnumerable<BatchPriceComponent> Components => m_components;

        public decimal TotalPriceInPrimaryCurrency => m_components.Where(c => c.Price != null).Sum(c => c.Price) ?? 0;

        public string Text
        {
            get
            {
                var sb= new StringBuilder();
                RenderText(sb);
                return sb.ToString();
            }
        }

        public void RenderText(StringBuilder target, int level = 0)
        {
            foreach (var c in Components)
            {
                if (!string.IsNullOrWhiteSpace(c.Text))
                {
                    target.Append(string.Empty.PadLeft(level, '\t')).AppendLine(c.Text);
                }

                foreach (var sp in c.ChildPrices)
                {
                    sp.RenderText(target, level + 1);
                }
            }
        }
    }
}
