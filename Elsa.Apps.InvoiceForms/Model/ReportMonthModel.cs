namespace Elsa.Apps.InvoiceForms.Model
{
    public class ReportMonthModel
    {
        public ReportMonthModel(int year, int month)
        {
            Year = year;
            Month = month;
        }

        public int Year { get; private set; }

        public int Month { get; private set; }

        public string Text => $"{Month.ToString().PadLeft(2, '0')}/{Year}";
    }
}
