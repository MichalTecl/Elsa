using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Integration.ChatGpt.Model
{
    public class OpenAiRequestBody
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "gpt-3.5-turbo";

        [JsonProperty("messages")]
        public List<Message> Messages { get; set; }

        [JsonProperty("temperature")]
        public float Temperature { get; set; } = 0.2f;

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 150;

        public class Message
        {
            [JsonProperty("role")]
            public string Role { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }
        }
    }
}
