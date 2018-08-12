using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Core.Entities.Commerce.Common;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;

namespace Elsa.Common.Configuration
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly ISession m_session;
        private readonly IDatabase m_database;

        private List<ISysConfig> m_allConfig;

        public ConfigurationRepository(ISession session, IDatabase database)
        {
            m_session = session;
            m_database = database;
        }
        
        public string GetJsonValue(int? projectId, int? userId, string key)
        {
            return GetEntry(projectId, userId, key)?.ValueJson;
        }

        public void SetJsonValue(int? projectId, int? userId, string key, string value)
        {
            using (var tran = m_database.OpenTransaction())
            {
                var entry = GetEntry(projectId, userId, key);

                if (entry != null && entry.ProjectId == projectId && entry.UserId == userId)
                {
                    if (entry.ValueJson == value)
                    {
                        return;
                    }

                    if (m_session?.User == null)
                    {
                        throw new UnauthorizedAccessException("Anonymous user cannot save a configuration");
                    }

                    entry.ValidTo = DateTime.Now;
                    m_allConfig.Remove(entry);
                    m_database.Save(entry);
                }

                var newEntry = m_database.New<ISysConfig>();

                newEntry.ProjectId = entry?.ProjectId ?? projectId;
                newEntry.UserId = entry?.UserId ?? userId;
                newEntry.InsertUserId = m_session.User.Id;
                newEntry.ValidFrom = DateTime.Now;
                newEntry.Key = key;
                newEntry.ValueJson = value;

                m_database.Save(newEntry);
                
                m_allConfig.Add(newEntry);

                tran.Commit();
            }
        }

        public T Load<T>(int? projectId, int? userId) where T : new()
        {
            var inst = new T();

            foreach (var prop in inst.GetType().GetProperties())
            {
                var keyAndDefault = ConfigEntryAttribute.GetConfigKeyAndDefault(prop);
                if (keyAndDefault == null)
                {
                    continue;
                }

                var value = GetJsonValue(projectId, userId, keyAndDefault.Item1) ?? keyAndDefault.Item2;

                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                var obj = JsonConvert.DeserializeObject(value, prop.PropertyType);

                prop.SetValue(inst, obj);
            }

            return inst;
        }

        public void Save<T>(int? projectId, int? userId, T configSet) where T : new()
        {
            foreach (var prop in configSet.GetType().GetProperties())
            {
                var keyAndDefault = ConfigEntryAttribute.GetConfigKeyAndDefault(prop);
                if (keyAndDefault == null)
                {
                    continue;
                }

                var objValue = prop.GetValue(configSet);
                var jsonValue = JsonConvert.SerializeObject(objValue);

                SetJsonValue(projectId, userId, keyAndDefault.Item1, jsonValue);
            }
        }

        private List<ISysConfig> GetConfiguration()
        {
            return m_allConfig ?? (m_allConfig = m_database.SelectFrom<ISysConfig>().Execute().ToList());
        }

        private ISysConfig GetEntry(int? projectId, int? userId, string key)
        {
            var allEntries = GetConfiguration().Where(i => i.Key == key && i.ValidFrom < DateTime.Now && (i.ValidTo ?? DateTime.Now.AddDays(10)) > DateTime.Now).ToList();

            var entry = allEntries.FirstOrDefault(i => i.InsertUserId == userId && i.ProjectId == projectId)
                        ?? allEntries.FirstOrDefault(i => i.ProjectId == projectId)
                        ?? allEntries.FirstOrDefault();

            return entry;
        }
    }
}

