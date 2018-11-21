using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core;
using Elsa.Common.Logging;
using Elsa.Jobs.Common;
using Elsa.Jobs.LoadCurrencyRates.Model;

namespace Elsa.Jobs.LoadCurrencyRates
{
    public class ImportRatesJob : IExecutableJob
    {
        private const string c_cnbRateLink = "https://www.cnb.cz/cs/financni_trhy/devizovy_trh/kurzy_devizoveho_trhu/denni_kurz.txt";
        private static readonly HttpClient s_client = new HttpClient();

        private readonly ILog m_log;
        private readonly ICurrencyRepository m_currencyRepository;

        public ImportRatesJob(ILog log, ICurrencyRepository currencyRepository)
        {
            m_log = log;
            m_currencyRepository = currencyRepository;
        }

        public void Run(string customDataJson)
        {
            using (var response = s_client.GetAsync(c_cnbRateLink).Result)
            {
                var text = response.Content.ReadAsStringAsync().Result;

                var rates = new RateSet(text);

                var currencies = m_currencyRepository.GetAllCurrencies().ToList();

                var mainCurrency = currencies.FirstOrDefault(c => c.IsProjectMainCurrency);
                if (mainCurrency == null)
                {
                    throw new InvalidOperationException("Project has no default currency defined");
                }
                
                foreach (var c in currencies.Where(c => !c.IsProjectMainCurrency))
                {
                    RateModel model;
                    if (!rates.Rates.TryGetValue(c.Symbol, out model))
                    {
                        continue;
                    }
                    
                    m_currencyRepository.SetCurrencyRate(c, mainCurrency, model.Rate / model.Amount, rates.RateDt, $"head:{rates.Header} link:{c_cnbRateLink}");
                }
            }
        }
    }
}
