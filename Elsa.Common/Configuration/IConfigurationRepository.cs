using System;
using System.Collections;
using System.Collections.Generic;

namespace Elsa.Common.Configuration
{
    public interface IConfigurationRepository
    {
        T Load<T>(int? projectId, int? userId) where T : new();

        object Load(Type t, int? projectId, int? userId);
        
        void Save<T>(int projectId, int userId, T configSet) where T : new();

        Dictionary<string, string> GetClientVisibleConfig();
    }
}
