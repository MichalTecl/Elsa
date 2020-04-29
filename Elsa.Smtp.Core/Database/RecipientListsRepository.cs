using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Robowire.RobOrm.Core;

namespace Elsa.Smtp.Core.Database
{
    public class RecipientListsRepository : IRecipientListsRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly ICache m_cache;

        public RecipientListsRepository(IDatabase database, ISession session, ICache cache)
        {
            m_database = database;
            m_session = session;
            m_cache = cache;
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
                using (var tx = m_database.OpenTransaction())
                {
                    var list = GetAllLists().FirstOrDefault(l => l.GroupName.Equals(groupName)) ??
                               m_database.New<IEmailRecipientList>(
                                   l =>
                                   {
                                       l.ProjectId = m_session.Project.Id;
                                       l.GroupName = groupName;
                                   });

                    list.Addresses = string.Join(";",
                        recipients.Select(r => r?.Trim()?.ToLowerInvariant())
                            .Where(r => !string.IsNullOrWhiteSpace(r)));

                    m_database.Save(list);

                    tx.Commit();
                }
            }
            finally
            {
                m_cache.Remove(GetCacheKey());
            }
        }

        public IEnumerable<string> GetAllGroupNames()
        {
            return GetAllLists().Select(g => g.GroupName);
        }

        private List<IEmailRecipientList> GetAllLists()
        {
            return m_cache.ReadThrough(GetCacheKey(), TimeSpan.FromDays(1),
                () => m_database.SelectFrom<IEmailRecipientList>().Where(m => m.ProjectId == m_session.Project.Id)
                    .OrderBy(m => m.GroupName).Execute().ToList()
            );
        }

        private string GetCacheKey()
        {
            return $"mailRecipients:{m_session.Project.Id}";
        }
    }
}
