namespace Elsa.Apps.InvoiceForms.Model
{
    public class PriceCalculationLog
    {
        public static readonly PriceCalculationLog Empty = new PriceCalculationLog("Výpočet ceny nedostupný", true);

        public PriceCalculationLog(string log, bool hasWarning)
        {
            Log = log;
            HasWarning = hasWarning;
        }

        public bool HasWarning { get; }

        public string Log { get; }

        public static PriceCalculationLog Get(string text, bool isWarning)
        {
            return string.IsNullOrWhiteSpace(text)
                ? Empty
                : new PriceCalculationLog(text, isWarning);
        }
    }
}
