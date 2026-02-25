using Elsa.Jobs.Common;
using Elsa.Jobs.CrmMailPull.Steps;
using System;

namespace Elsa.Jobs.CrmMailPull
{
    public class MailPullJob : IExecutableJob
    {
        private readonly ExploreFolders _exploreFolders;
        private readonly LoadMailReferences _loadMailReferences;
        private readonly LoadMessagesContent _loadMessagesContent;

        public MailPullJob(ExploreFolders exploreFolders, LoadMailReferences loadMailReferences, LoadMessagesContent loadMessagesContent)
        {
            _exploreFolders = exploreFolders;
            _loadMailReferences = loadMailReferences;
            _loadMessagesContent = loadMessagesContent;
        }

        public void Run(string customDataJson)
        {
            _exploreFolders.Explore();
            _loadMailReferences.Load();
            _loadMessagesContent.Load();
        }
    }
}
