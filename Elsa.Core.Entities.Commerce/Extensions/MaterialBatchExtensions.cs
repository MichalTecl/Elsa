using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Core.Entities.Commerce.Extensions
{
    public static class MaterialBatchExtensions
    {
        public static string GetTextInfo(this IMaterialBatch batch)
        {
            return string.Join(" ", GetBatchInfo(batch));
        }

        public static string GetUnid(this IMaterialBatch batch)
        {
            return $"{batch.BatchNumber}.{batch.Id}";
        }

        private static IEnumerable<string> GetBatchInfo(IMaterialBatch b)
        {
            if (b == null)
            {
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(b.BatchNumber))
            {
                yield return b.GetUnid();
            }
            else
            {
                yield return $"sysID:{b.Id}";
            }

            if (b.Unit != null)
            {
                yield return $"{b.Volume}{b.Unit.Symbol}";
            }

            if (b.Material != null)
            {
                yield return $"{b.Material.Name}";
            }

            if (!string.IsNullOrWhiteSpace(b.InvoiceNr))
            {
                yield return $"f.:{b.InvoiceNr}";
            }

            if (!string.IsNullOrWhiteSpace(b.InvoiceVarSymbol))
            {
                yield return $"vs.:{b.InvoiceVarSymbol}";
            }

            yield return b.Created.ToString("dd.MM.yyyy");
        }
    }
}
