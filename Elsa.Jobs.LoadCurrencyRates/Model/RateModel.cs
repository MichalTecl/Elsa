using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.LoadCurrencyRates.Model
{
    public class RateModel
    {
        public RateModel(string sourceLine)
        {
            var cells = sourceLine.Split('|');
            if (cells.Length != 5)
            {
                throw new InvalidOperationException($"Unexpected format of source line {sourceLine}");
            }

            Symbol = cells[3];
            Amount = decimal.Parse(cells[2].Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            Rate = decimal.Parse(cells[4].Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
        }

        public string Symbol { get; }

        public decimal Amount { get; }

        public decimal Rate { get; }
    }
}
