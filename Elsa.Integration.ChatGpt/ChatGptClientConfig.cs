using Elsa.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Integration.ChatGpt
{
    [ConfigClass]
    public class ChatGptClientConfig
    {
        [ConfigEntry("ChatGpt.ApiKey", ConfigEntryScope.Project)]
        public string ApiKey { get; set; }
    }
}
