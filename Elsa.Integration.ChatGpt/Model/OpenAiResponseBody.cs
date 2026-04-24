using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Integration.ChatGpt.Model
{
    public class OpenAiResponseBody
    {
        public List<Choice> Choices { get; set; }

        public class Choice
        {
            [JsonProperty("finish_reason")]
            public string FinishReason { get; set; }

            public Message Message { get; set; }
        }

        public class Message
        {
            public string Role { get; set; }
            public string Content { get; set; }

            public string Refusal { get; set; }
        }
    }

}
