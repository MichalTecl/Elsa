using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Elsa.Integration.Erp.Fler.Model
{
    public class SessionModel
    {
        [JsonProperty("secret_key")]
        public string SecretKey { get; set; }

        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("authmode")]
        public string AuthMode { get; set; }
    }
}
