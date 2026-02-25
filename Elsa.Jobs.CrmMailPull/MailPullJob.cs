using Elsa.Jobs.Common;
using Elsa.Jobs.CrmMailPull.Steps;
using System;

namespace Elsa.Jobs.CrmMailPull
{
    public class MailPullJob : IExecutableJob
    {
        private readonly ExploreFolders _exploreFolders;

        public MailPullJob(ExploreFolders exploreFolders)
        {
            _exploreFolders = exploreFolders;
        }

        public void Run(string customDataJson)
        {
            _exploreFolders.Explore();
        }
    }
}
