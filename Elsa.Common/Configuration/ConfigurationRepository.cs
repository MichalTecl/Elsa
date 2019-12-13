using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common.Caching;
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
        private readonly ICache m_cache;
        private readonly ISession m_session;

        public ConfigurationRepository(IDatabase database, ILog log, ICache cache, ISession session)
        {
            m_database = database;
            m_log = log;
            m_cache = cache;
            m_session = session;
        }
      
        public T Load<T>(int? projectId, int? userId) where T : new()
        {
            return (T)Load(typeof(T), projectId, userId);
        }

        public object Load(Type t, int? projectId, int? userId)
        {
            var cacheKey = $"config_class_{t.FullName}_{projectId}_{userId}";

            return m_cache.ReadThrough(cacheKey,
                TimeSpan.FromHours(1),
                () =>
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
                        throw new InvalidOperationException(
                            $"Requested configuration type {t} doesn't have any configuration property (marked by {typeof(ConfigEntryAttribute)})");
                    }

                    return inst;
                });
        }

        public void Save<T>(int projectId, int userId, T configSet) where T : new()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> GetClientVisibleConfig()
        {
            var entries = m_cache.ReadThrough("global_clivissettings", TimeSpan.FromDays(1),
                () => m_database.SelectFrom<ISysConfig>().Where(c => c.ClientSideVisible == true).Execute()
                    .ToList()).Where(e => e.ValidFrom < DateTime.Now && (e.ValidTo ?? DateTime.Now.AddDays(1)) > DateTime.Now).OrderBy(GetEntryPriority).ToList();

            var result = new Dictionary<string, string>();
            foreach (var entry in entries)
            {
                result[entry.Key] = entry.ValueJson;
            }

            return result;
        }

        public void Save<T>(int? projectId, int? userId, T configSet) where T : new()
        {
            throw new NotImplementedException();
        }
        
        private List<ISysConfig> GetConfiguration()
        {
            var now = DateTime.Now;
            var future = DateTime.Now.AddDays(100);

            return GetAllConfiguration().Where(i => (i.ValidFrom < now) && ((i.ValidTo ?? future) > now)).ToList();
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
                        entry = allEntries.FirstOrDefault(i => (i.ProjectId == projectId) && (i.UserId == userId));
                        break;
                    case ConfigEntryScope.Project:
                        entry = allEntries.FirstOrDefault(i => (i.UserId == null) && (i.ProjectId == projectId));
                        break;
                    case ConfigEntryScope.Global:
                        entry = allEntries.FirstOrDefault(i => (i.ProjectId == null) && (i.UserId == null));
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

        private List<ISysConfig> GetAllConfiguration()
        {
            return m_cache.ReadThrough($"All_sysConfig",
                TimeSpan.FromHours(1),
                () => m_database.SelectFrom<ISysConfig>().Execute().ToList());
        }

        private static int GetEntryPriority(ISysConfig entry)
        {
            if (entry.UserId != null)
            {
                return 3;
            }

            if (entry.ProjectId != null)
            {
                return 2;
            }
            
            return 1;
        }
    }
}

