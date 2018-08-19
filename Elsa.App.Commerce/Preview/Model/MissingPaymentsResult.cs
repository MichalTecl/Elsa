namespace Elsa.App.Commerce.Preview.Model
{
    public class MissingPaymentsResult
    {
        public MissingPaymentsResult(int businessDaysTolerance, int count)
        {
            BusinessDaysTolerance = businessDaysTolerance;
            Count = count;
        }

        public int BusinessDaysTolerance { get; }

        public int Count { get; }
    }
}
