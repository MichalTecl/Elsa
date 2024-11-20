using Elsa.Integration.ChatGpt.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Integration.ChatGpt
{
    public interface IChatGptClient
    {
        OpenAiResponseBody Request(OpenAiRequestBody body);
    }
}
