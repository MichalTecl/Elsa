using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.Apps.CommonData
{
    [Controller("currency")]
    public class CurrencyController : ElsaControllerBase
    {
        private readonly ICurrencyRepository m_currencyRepository;

        public CurrencyController(IWebSession webSession, ILog log, ICurrencyRepository currencyRepository)
            : base(webSession, log)
        {
            m_currencyRepository = currencyRepository;
        }

        public IEnumerable<string> GetSymbols()
        {
            return m_currencyRepository.GetAllCurrencies().Select(c => c.Symbol).OrderBy(i => i);
        }
    }
}
