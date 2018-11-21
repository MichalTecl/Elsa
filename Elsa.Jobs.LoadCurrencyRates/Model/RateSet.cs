using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.LoadCurrencyRates.Model
{
    public class RateSet
    {

        public RateSet(string file)
        {
            var lines = file.Split(new char[] { '\r', '\n' }).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            Rates = new Dictionary<string, RateModel>(lines.Length);

            for (var i = 0; i < lines.Length; i++)
            {
                if (i == 1)
                {
                    continue;
                }

                if (i == 0)
                {
                    Header = lines[i];
                    var parts = Header.Split(new char[] { ' ', '#' });
                    RateDt = DateTime.ParseExact(parts[0], "dd.MM.yyyy", CultureInfo.InvariantCulture);
                    continue;
                }

                var rateModel = new RateModel(lines[i]);
                Rates[rateModel.Symbol] = rateModel;
            }
        }

        public DateTime RateDt { get; }
        public string Header { get; }

        public IDictionary<string, RateModel> Rates { get; }
    }
}
