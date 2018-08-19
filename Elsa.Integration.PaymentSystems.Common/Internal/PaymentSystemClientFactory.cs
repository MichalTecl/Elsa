using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Integration.PaymentSystems.Common.Internal
{
    public class PaymentSystemClientFactory : IPaymentSystemClientFactory
    {
        private readonly IServiceLocator m_locator;
        private readonly ISession m_session;
        private readonly IDatabase m_database;

        public PaymentSystemClientFactory(IServiceLocator locator, ISession session, IDatabase database)
        {
            m_locator = locator;
            m_session = session;
            m_database = database;
        }

        public IPaymentSystemClient GetClient(IPaymentSource source)
        {
            var client = m_locator.InstantiateNow<IPaymentSystemClient>(source.ClientClass);
            client.Entity = source;

            return client;
        }

        public IPaymentSystemHub GetPaymentSystems()
        {
            return new PaymentSysHub(GetAllPaymentSystemClients());
        }

        public IEnumerable<IPaymentSystemClient> GetAllPaymentSystemClients()
        {
            return m_database.SelectFrom<IPaymentSource>().Where(p => p.ProjectId == m_session.Project.Id).Execute().Select(GetClient);
        }
    }
}
