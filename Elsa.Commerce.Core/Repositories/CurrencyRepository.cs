using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class CurrencyRepository : ICurrencyRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        private readonly List<ICurrency> m_index = new List<ICurrency>();

        public CurrencyRepository(IDatabase database, ISession session)
        {
            m_database = database;
            m_session = session;
        }

        public ICurrency GetCurrency(string symbol)
        {
            var currency = m_index.FirstOrDefault(c => c.Symbol == symbol && c.ProjectId == m_session.Project.Id);
            if (currency == null)
            {
                currency =
                    m_database.SelectFrom<ICurrency>()
                        .Where(c => c.ProjectId == m_session.Project.Id && c.Symbol == symbol)
                        .Execute()
                        .FirstOrDefault();

                m_index.Add(currency);
            }

            return currency;
        }

        public void SaveCurrency(ICurrency currency)
        {
            var cachedCurrency = m_index.FirstOrDefault(c => c.Symbol == currency.Symbol && c.ProjectId == m_session.Project.Id);
            if (cachedCurrency != null && currency.Id > 0 && cachedCurrency.Id == currency.Id
                && cachedCurrency.Symbol == currency.Symbol)
            {
                return;
            }


            m_index.Clear();

            if (currency.Id > 0 && currency.ProjectId != m_session.Project.Id)
            {
                throw new InvalidOperationException("Project assignment mischmatch");
            }

            currency.ProjectId = m_session.Project.Id;

            m_database.Save(currency);
        }
    }
}
