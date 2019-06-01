
namespace Elsa.Apps.CommonData.Model
{
    public class FixedCostValueViewModel
    {
        public int TypeId { get; set; }

        public string TypeName { get; set; }

        public decimal Value { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public string Uid => $"{TypeId}:{Year}:{Month}";
    }
}
