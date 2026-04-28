using Elsa.Jobs.CrmMailPull.Infrastructure;
using Elsa.Jobs.CrmMailPull.Steps;
using Robowire;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.CrmMailPull
{
    public class CrmMailPullRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<MailPullRepository>().Use<MailPullRepository>();
            setup.For<MailConversationSummarizer>().Use<MailConversationSummarizer>();
            setup.For<ExploreFolders>().Use<ExploreFolders>();
            setup.For<LoadMailReferences>().Use<LoadMailReferences>();
            setup.For<LoadMessagesContent>().Use<LoadMessagesContent>();
            setup.For<CompleteConversations>().Use<CompleteConversations>();
            setup.For<SummarizeConversations>().Use<SummarizeConversations>();
        }
    }
}
