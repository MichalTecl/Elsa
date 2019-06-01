using System;

namespace Elsa.Apps.CommonData.Model
{
    public class AccMonthModel
    {
        public AccMonthModel(DateTime dt)
        {
            Year = dt.Year;
            Month = dt.Month;
        }

        public int Year { get; set; }

        public int Month { get; set; }

        public string Text => $"{Month.ToString().PadLeft(2, '0')}/{Year}";
    }
}
