using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using RobOrm.Core;

namespace Elsa.Smtp.Core.Database
{
    public class RecipientListsRepository : IRecipientListsRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ICache _cache;

        public RecipientListsRepository(IDatabase database, ISession session, ICache cache)
        {
            _database = database;
            _session = session;
            _cache = cache;
        }

        public IEnumerable<string> GetRecipients(string groupName)
        {
            var list = GetAllLists()
                .FirstOrDefault(l => l.GroupName.Equals(groupName));

            if (list == null)
            {
                SetRecipeints(groupName, new string[0]);
                return new List<string>(0);
            }

            return list.Addresses.Split(';');
        }

        public void SetRecipeints(string groupName, IEnumerable<string> recipients)
        {
            try
            {
                using (var tx = _database.OpenTransaction())
                {
                    var list = GetAllLists().FirstOrDefault(l => l.GroupName.Equals(groupName)) ??
                               _database.New<IEmailRecipientList>(
                                   l =>
                                   {
                                       l.ProjectId = _session.Project.Id;
                                       l.GroupName = groupName;
                                   });

                    list.Addresses = string.Join(";",
                        recipients.Select(r => r?.Trim()?.ToLowerInvariant())
                            .Where(r => !string.IsNullOrWhiteSpace(r)));

                    _database.Save(list);

                    tx.Commit();
                }
            }
            finally
            {
                _cache.Remove(GetCacheKey());
            }
        }

        public IEnumerable<string> GetAllGroupNames()
        {
            return GetAllLists().Select(g => g.GroupName);
        }

        private List<IEmailRecipientList> GetAllLists()
        {
            return _cache.ReadThrough(GetCacheKey(), TimeSpan.FromDays(1),
                () => _database.SelectFrom<IEmailRecipientList>().Where(m => m.ProjectId == _session.Project.Id)
                    .OrderBy(m => m.GroupName).Execute().ToList()
            );
        }

        private string GetCacheKey()
        {
            return $"mailRecipients:{_session.Project.Id}";
        }
    }
}
