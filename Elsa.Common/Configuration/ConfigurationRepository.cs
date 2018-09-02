using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;

namespace Elsa.Common.Configuration
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly ILog m_log;

        private readonly IDatabase m_database;

        private List<ISysConfig> m_allConfig;

        public ConfigurationRepository(IDatabase database, ILog log)
        {
            m_database = database;
            m_log = log;
        }
      
        public T Load<T>(int? projectId, int? userId) where T : new()
        {
            return (T)Load(typeof(T), projectId, userId);
        }

        public object Load(Type t, int? projectId, int? userId)
        {
            var inst = Activator.CreateInstance(t);

            var propFound = false;

            foreach (var prop in inst.GetType().GetProperties())
            {
                var definition = ConfigEntryAttribute.GetDefinition(prop);
                if (definition == null)
                {
                    continue;
                }
                propFound = true;

                var entry = GetEntry(projectId, userId, definition);

                var json = entry?.ValueJson;
                if (string.IsNullOrEmpty(json))
                {
                    continue;
                }

                var obj = JsonConvert.DeserializeObject(json, prop.PropertyType);

                prop.SetValue(inst, obj);
            }

            if (!propFound)
            {
                throw new InvalidOperationException($"Requested configuration type {t} doesn't have any configuration property (marked by {typeof(ConfigEntryAttribute)})");
            }

            return inst;
        }

        public void Save<T>(int projectId, int userId, T configSet) where T : new()
        {
            throw new NotImplementedException();
        }

        public void Save<T>(int? projectId, int? userId, T configSet) where T : new()
        {
            throw new NotImplementedException();
        }
        
        private List<ISysConfig> GetConfiguration()
        {
            var now = DateTime.Now;
            var future = DateTime.Now.AddDays(100);

            return m_allConfig ?? (m_allConfig = m_database.SelectFrom<ISysConfig>()
                      .Where(i => i.ValidFrom < now
                         && (i.ValidTo ?? future) > now)
                         .Execute().ToList());
        }
        
        private ISysConfig GetEntry(int? projectId, int? userId, IConfigEntryDefinition definition)
        {
            var allEntries = GetConfiguration().Where(i => i.Key == definition.Key).ToList(); 
                         
            ISysConfig entry = null;
            foreach(var scope in definition.Scope)
            {
                switch(scope)
                {
                    case ConfigEntryScope.User:
                        entry = allEntries.FirstOrDefault(i => i.ProjectId == projectId && i.UserId == userId);
                        break;
                    case ConfigEntryScope.Project:
                        entry = allEntries.FirstOrDefault(i => i.UserId == null && i.ProjectId == projectId);
                        break;
                    case ConfigEntryScope.Global:
                        entry = allEntries.FirstOrDefault(i => i.ProjectId == null && i.UserId == null);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (entry != null)
                {
                    break;
                }
            }

            if (entry == null)
            {
                m_log.Error($"Config entry {definition.Key} not found. ProjectId={projectId}, UserId={userId}");
            }

            return entry;
        }
    }
}

