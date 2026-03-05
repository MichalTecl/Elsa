using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Text;

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
            
            var res = _db.Sql()
               .Call("DeleteIncompleteMailConversations")
               .Scalar<int>();

            _log.Info($"Deleted {res} of incomplete conversations ( = where additonal mail was received after the conversation was completed)");

        }
    }
}
