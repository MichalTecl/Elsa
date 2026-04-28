using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Jobs.Common;
using Elsa.Jobs.CrmMailPull.Steps;
using System;
using System.Diagnostics;

namespace Elsa.Jobs.CrmMailPull
{
    public class MailPullJob : IExecutableJob
    {
#if DEBUG
        public const bool SKIP_AI_SUMMARISATION = true;
#endif

#if !DEBUG
        public const bool SKIP_AI_SUMMARISATION = false;
#endif

        private readonly IStep[] _steps;
        private readonly ILog _log;

        public MailPullJob(
            ILog log,
            ExploreFolders exploreFolders,
            LoadMailReferences loadMailReferences,
            LoadMessagesContent loadMessagesContent,
            CompleteConversations completeConversations,
            SummarizeConversations summarizeConversations)
        {
            _log = log;

            _steps = new IStep[]
            {
                exploreFolders,
                loadMailReferences, 
                loadMessagesContent,
                completeConversations,
                summarizeConversations
            };
        }

        public void Run(string customDataJson)
        {
            var timeout = new TimeoutCheck(TimeSpan.FromMinutes(90));

            foreach(var step in _steps)
            {
                _log.Info($"Executing step {step.GetType().Name}");

                try
                {
                    step.Run(timeout);

                    _log.Info($"Step {step.GetType().Name} completed");
                }
                catch (Exception ex)
                {
                    _log.Error($"Step {step.GetType().Name} failed", ex);
                }
            }
        }
    }
}
