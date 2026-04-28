using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Jobs.CrmMailPull.Infrastructure;
using System;

namespace Elsa.Jobs.CrmMailPull.Steps
{
    public class SummarizeConversations : IStep
    {
        private readonly ILog _log;
        private readonly MailPullRepository _repository;
        private readonly MailConversationSummarizer _summarizer;

        public SummarizeConversations(ILog log, MailPullRepository repository, MailConversationSummarizer summarizer)
        {
            _log = log;
            _repository = repository;
            _summarizer = summarizer;
        }

        public void Run(TimeoutCheck timeout)
        {
            SummaryPromptInfo summaryPrompt = _repository.GetLatestConfirmedSummaryPrompt();

            if (string.IsNullOrWhiteSpace(summaryPrompt?.Prompt))
                throw new InvalidOperationException("No confirmed mail conversation summary prompt found in database");

            while (true)
            {
                timeout.Check();

                var conversationId = _repository.GetNewestConversationIdMissingSummary();
                if (conversationId == null)
                {
                    _log.Info("No conversations missing summary");
                    return;
                }

                var conversationIdVal = conversationId.Value;
                _log.Info($"Summarizing conversation Id={conversationIdVal}");

                try
                {
                    var result = _summarizer.SummarizeConversation(
                        conversationIdVal,
                        summaryPrompt?.Prompt,
                        skipAi: false);

                    _repository.SaveConversationSummary(
                        conversationIdVal,
                        result.SubjectSummary,
                        result.Summary,
                        result.UsedAi ? summaryPrompt?.Id : null);

                    _log.Info($"Conversation {conversationIdVal} summarized");
                }
                catch (Exception ex)
                {
                    _log.Error($"Summarization failed for conversation {conversationIdVal}", ex);
                    break;
                }
            }
        }
    }
}
