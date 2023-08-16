using Elsa.Common.Configuration;
using System;

namespace Elsa.Integration.Crm.Raynet
{
    [ConfigClass]
    public class RaynetClientConfig
    {
        [ConfigEntry("RayNet.UserName", ConfigEntryScope.Project)]
        public string UserName { get; set; }

        [ConfigEntry("RayNet.InstanceName", ConfigEntryScope.Project)]
        public string InstanceName { get; set; }

        [ConfigEntry("RayNet.ApiKey", ConfigEntryScope.Project)]
        public string ApiKey { get; set; }
    }
}
