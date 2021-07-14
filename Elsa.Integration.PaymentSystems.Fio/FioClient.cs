using System;
using System.Collections.Generic;

using Elsa.Commerce.Core;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.PaymentSystems.Common;
using Elsa.Integration.PaymentSystems.Fio.Internal;

namespace Elsa.Integration.PaymentSystems.Fio
{
    public class FioClient : IPaymentSystemClient
    {
        private readonly FioClientConfig m_config;
        private readonly ICurrencyRepository m_currencyRepository;
        private readonly ILog m_log;

        public FioClient(FioClientConfig config, ICurrencyRepository currencyRepository, ILog log)
        {
            m_config = config;
            m_currencyRepository = currencyRepository;
            m_log = log;
        }

        public IPaymentSource Entity { get; set; }

        public IPaymentSystemCommonSettings CommonSettings => m_config;

        public IEnumerable<IPayment> GetPayments(DateTime from, DateTime to)
        {
            string token = null;
            if (!m_config.Tokens?.TryGetValue(Entity.Description, out token) ?? false)
            {
                throw new InvalidOperationException($"Konfigurace FIO.Tokens neni nastavena nebo neobsahuje hodnotu pojmenovanou \"{Entity.Description}\"");
            }

            var client = new FioApiClient(m_log);

            foreach (var p in client.LoadPayments(token, from, to))
            {
                var model = new PaymentModel(p, m_currencyRepository) { PaymentSourceId = Entity.Id };

                yield return model;
            }
        }
    }
}
