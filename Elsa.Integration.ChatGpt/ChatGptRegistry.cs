using Robowire;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Integration.ChatGpt
{
    public class ChatGptRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IChatGptClient>().Use<ChatGptClient>();
        }
    }
}
