using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire;
using RobOrm.Core;

namespace Elsa.Integration.PaymentSystems.Common.Internal
{
    public class PaymentSystemClientFactory : IPaymentSystemClientFactory
    {
        private readonly IServiceLocator _locator;
        private readonly ISession _session;
        private readonly IDatabase _database;

        public PaymentSystemClientFactory(IServiceLocator locator, ISession session, IDatabase database)
        {
            _locator = locator;
            _session = session;
            _database = database;
        }

        public IPaymentSystemClient GetClient(IPaymentSource source)
        {
            var client = _locator.InstantiateNow<IPaymentSystemClient>(source.ClientClass);
            client.Entity = source;

            return client;
        }

        public IPaymentSystemHub GetPaymentSystems()
        {
            return new PaymentSysHub(GetAllPaymentSystemClients());
        }

        public IEnumerable<IPaymentSystemClient> GetAllPaymentSystemClients()
        {
            return _database.SelectFrom<IPaymentSource>().Where(p => p.ProjectId == _session.Project.Id && (p.IsActive != false)).Execute().Select(GetClient);
        }
    }
}
