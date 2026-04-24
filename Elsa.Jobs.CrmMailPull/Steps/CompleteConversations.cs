using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RobOrm.Core;

namespace Elsa.Jobs.CrmMailPull.Steps
{
    public class CompleteConversations : IStep
    {
        private readonly IDatabase _db;
        private readonly ILog _log;

        public CompleteConversations(IDatabase db, ILog log)
        {
            _db = db;
            _log = log;
        }

        public void Run(TimeoutCheck timeout)
        {
            _log.Info("Deleting incomplete conversations");
            
            var deletedCount = _db.Sql()
               .Call("DeleteIncompleteMailConversations")
               .Scalar<int>();

            _log.Info($"Deleted {deletedCount} of incomplete conversations ( = where additonal mail was received after the conversation was completed)");

            _log.Info("Completing conversations from loaded mail contents");

            var createdCount = 0;
            var assignedCount = 0;

            _db.Sql()
                .Call("CompleteMailConversations")
                .ReadRows<int, int>((created, assigned) =>
                {
                    createdCount = created;
                    assignedCount = assigned;
                });

            _log.Info($"Created {createdCount} conversation(s) and assigned {assignedCount} message(s)");

        }
    }
}
