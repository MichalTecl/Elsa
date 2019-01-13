using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

using Newtonsoft.Json;

namespace Elsa.Commerce.Core.Model
{
    public class ProductionStepViewModel
    {
        public HashSet<int> BatchIds { get; set; } = new HashSet<int>();
        public int MaterialId { get; set; }
        public int MaterialProductionStepId { get; set; }
        public string StepName { get; set; }

        public bool RequiresPrice { get; set; }
        public bool RequiresTime { get; set; }
        public bool RequiresWorkerReference { get; set; }
        public bool IsAutoBatch { get; set; }

        public decimal? Hours { get; set; }
        public decimal? Price { get; set; }
        public string Worker { get; set; }

        public string ValidationErrors { get; set; }
        public decimal Quantity { get; set; }

        public decimal MaxQuantity { get; set; }
        public string UnitSymbol { get; set; }

        [JsonIgnore]
        public IMaterialUnit Unit { get; set; }

        public string MaterialName { get; set; }
        public string BatchNumber { get; set; }
        
        public List<MaterialBatchResolutionModel> Materials { get; } = new List<MaterialBatchResolutionModel>();

        [JsonIgnore]
        public IMaterial BatchMaterial { get; set; }

        [JsonIgnore]
        public IMaterialProductionStep MaterialStep { get; set; }

        public bool IsValid { get; set; }
        public bool NeedsBatchNumber { get; set; }

        public static IEnumerable<ProductionStepViewModel> JoinAutomaticMaterials(List<ProductionStepViewModel> source)
        {
            var result = new List<ProductionStepViewModel>(source.Count);
            ProductionStepViewModel lastStep = null;
            foreach (var step in source.OrderBy(r => r.MaterialProductionStepId))
            {
                if (lastStep?.MaterialProductionStepId == step.MaterialProductionStepId && step.IsAutoBatch)
                {
                    lastStep.JoinWith(step);
                    continue;
                }

                result.Add(step);
                lastStep = step;
            }

            return result;
        }

        public bool IsSameStep(ProductionStepViewModel other)
        {
            if (MaterialId != other.MaterialId || MaterialProductionStepId != other.MaterialProductionStepId)
            {
                return false;
            }

            if (BatchIds.Count != other.BatchIds.Count)
            {
                return false;
            }

            return BatchIds.All(myBid => other.BatchIds.Contains(myBid));
        }

        private void JoinWith(ProductionStepViewModel step)
        {
            BatchNumber = null;

            foreach (var bid in step.BatchIds)
            {
                BatchIds.Add(bid);
            }

            Quantity = Quantity + step.Quantity;
        }

        public IEnumerable<MaterialBatchResolutionModel> ReleaseMaterialForAmount(decimal amount)
        {
            if (amount > Quantity)
            {
                throw new InvalidOperationException("Invalid operation");
            }

            if (Math.Abs(Quantity - amount) < 0.00001m)
            {
                foreach (var m in Materials)
                {
                    yield return m;
                }

                Materials.Clear();
                Quantity = 0;

                yield break;
            }

            var factor = amount / Quantity;

            var allMaterialIds = Materials.Select(m => m.MaterialId).Distinct().ToList();

            foreach (var materialId in allMaterialIds)
            {
                var allRecordsForThisMaterial = Materials.Where(m => m.MaterialId == materialId).ToList();

                var requiredAmount = allRecordsForThisMaterial.Sum(m => m.Amount) * factor;

                while (requiredAmount > 0)
                {
                    var entryToUse = allRecordsForThisMaterial.FirstOrDefault(r => r.Amount <= requiredAmount) ??
                                     allRecordsForThisMaterial.FirstOrDefault();
                    if (entryToUse == null)
                    {
                        throw new InvalidOperationException("nejak jsem se nedopocital :(");
                    }

                    Materials.Remove(entryToUse);

                    var availableAmountForThisEntry = Math.Min(requiredAmount, entryToUse.Amount);

                    allRecordsForThisMaterial.Remove(entryToUse);
                    Materials.Remove(entryToUse);

                    var toReturn = entryToUse.Clone();
                    toReturn.Amount = availableAmountForThisEntry;
                    yield return toReturn;

                    var remaining = entryToUse.Amount - availableAmountForThisEntry;
                    if (Math.Abs(remaining) > 0.00001m)
                    {
                        var toStay = entryToUse.Clone();
                        toStay.Amount = entryToUse.Amount - availableAmountForThisEntry;
                        Materials.Add(toStay);
                        allRecordsForThisMaterial.Add(toStay);
                    }

                    requiredAmount -= toReturn.Amount;
                }
            }
        }
    }
}
