using System.Collections.Generic;

using Elsa.Common.Configuration;

namespace Elsa.Integration.PaymentSystems.Fio
{
    [ConfigClass]
    public class FioClientConfig
    {
        [ConfigEntry("FIO.Tokens", ConfigEntryScope.Project)]
        public Dictionary<string, string> Tokens { get; set; }
    }
}
