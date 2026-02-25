using Elsa.Jobs.Common;
using Elsa.Jobs.CrmMailPull.Steps;
using System;

namespace Elsa.Jobs.CrmMailPull
{
    public class MailPullJob : IExecutableJob
    {
        private readonly ExploreFolders _exploreFolders;
        private readonly LoadMailReferences _loadMailReferences;

        public MailPullJob(ExploreFolders exploreFolders, LoadMailReferences loadMailReferences)
        {
            _exploreFolders = exploreFolders;
            _loadMailReferences = loadMailReferences;
        }

        public void Run(string customDataJson)
        {
            _exploreFolders.Explore();
            _loadMailReferences.Load();
        }
    }
}
