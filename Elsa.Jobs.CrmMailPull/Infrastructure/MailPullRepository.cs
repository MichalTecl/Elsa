using Elsa.Common.Caching;
using Elsa.Jobs.CrmMailPull.Entities;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elsa.Jobs.CrmMailPull.Infrastructure
{
    public class MailPullRepository
    {
        private readonly ICache _cache;
        private readonly IDatabase _database;

        public MailPullRepository(ICache cache, IDatabase database)
        {
            _cache = cache;
            _database = database;
        }

        public List<IMailboxFolder> GetActiveMailFolders()
        {
            return _cache.ReadThrough("activeMailFolders",
                TimeSpan.FromHours(1),
                () => _database.SelectFrom<IMailboxFolder>()
                .Join(f => f.MailPullSource)
                .Where(f => f.IsEnabled && f.MailPullSource.IsEnabled)
                .Execute()
                .ToList());
        }

        public List<IMailPullSource> GetActiveSources()
        {
            return _database.SelectFrom<IMailPullSource>()
                .Join(s => s.Folders)
                .Where(s => s.IsEnabled)
                .Execute()
                .ToList();
        }
    }
}
