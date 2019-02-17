namespace Elsa.Commerce.Core.Model
{
    public class MaterialLevelModel
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; }

        public decimal? MinValue { get; set; } 

        public decimal MaxValue { get; set; }

        public decimal ActualValue { get; set; }

        public int UnitId { get; set; }

        public string Unit { get; set; }

        public int PercentLevel { get; set; }

        public bool HasThreshold { get; set; }
    }
}
