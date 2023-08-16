using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Jobs.Common.EntityChangeProcessing.Entities;
using Robowire.RobOrm.Core;

namespace Elsa.Jobs.Common.EntityChangeProcessing.Helpers
{
    public class ChangeProcessorHostFactory : IChangeProcessorHostFactory
    {
        private readonly ILog _log;
        private readonly IDatabase _db;
        private readonly ISession _session;

        public ChangeProcessorHostFactory(ILog log, IDatabase db, ISession session)
        {
            _log = log;
            _db = db;
            _session = session;
        }

        public IChangeProcessorHost<T> Get<T>()
        {
            return new ChangeProcessorHost<T>(_log, _db, _session);
        }
    }
}
