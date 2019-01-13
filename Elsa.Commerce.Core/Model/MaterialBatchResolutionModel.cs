namespace Elsa.Commerce.Core.Model
{
    public class MaterialBatchResolutionModel
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; }

        public string UnitSymbol { get; set; }

        public decimal Amount { get; set; }

        public string BatchNumber { get; set; }
        public bool AutomaticBatches { get; set; }

        public MaterialBatchResolutionModel Clone()
        {
            return new MaterialBatchResolutionModel
            {
                MaterialId = MaterialId,
                MaterialName = MaterialName,
                UnitSymbol = UnitSymbol,
                Amount = Amount,
                BatchNumber = BatchNumber,
                AutomaticBatches = AutomaticBatches
            };
        }
    }
}
