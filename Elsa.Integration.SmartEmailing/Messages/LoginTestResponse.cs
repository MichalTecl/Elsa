using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartEmailingApi.Client.Messages
{
    public class LoginTestResponse : SeResponse
    {
        [JsonPropertyName("account_id")]
        public int AccountId { get; set; }
    }
}
