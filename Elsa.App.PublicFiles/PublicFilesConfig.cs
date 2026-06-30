using Elsa.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.PublicFiles
{
    [ConfigClass]
    public class PublicFilesConfig
    {
        [ConfigEntry("CloudFlare.ZoneId", ConfigEntryScope.Project)]
        public string CfZoneId { get; set; }

        [ConfigEntry("CloudFlare.ApiToken", ConfigEntryScope.Project)]
        public string CfToken { get; set; }
    }
}
