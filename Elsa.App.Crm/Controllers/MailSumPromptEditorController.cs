using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Jobs.CrmMailPull.Entities;
using Elsa.Jobs.CrmMailPull.Infrastructure;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.App.Crm.Controllers
{
    [Controller("MailSumPromptEditor")]
    public class MailSumPromptEditorController : ElsaControllerBase
    {
        private static readonly DateTime NonConfirmedPromptDt = new DateTime(1900, 1, 1);

        private readonly IDatabase _db;
        private readonly MailConversationSummarizer _summarizer;
        private readonly CustomerMeetingsRepository _meetingsRepository;
        private readonly MailConversationMeetingModelFactory _mailConversationMeetingModelFactory;

        public MailSumPromptEditorController(
            IWebSession webSession,
            ILog log,
            IDatabase db,
            MailConversationSummarizer summarizer,
            CustomerMeetingsRepository meetingsRepository,
            MailConversationMeetingModelFactory mailConversationMeetingModelFactory) : base(webSession, log)
        {
            _db = db;
            _summarizer = summarizer;
            _meetingsRepository = meetingsRepository;
            _mailConversationMeetingModelFactory = mailConversationMeetingModelFactory;
        }

        protected override void OnBeforeCall()
        {
            base.OnBeforeCall();
            EnsureUserRight(CrmUserRights.EmailSummaryPromptEdit);
        }

        public List<PromptInfo> GetPrompts()
        {
            var all = _db.SelectFrom<IMailConversationSummaryPrompt>()
                .Join(p => p.Author)
                .OrderByDesc(p => p.CreateDt)
                .Execute()
                .ToList();

            var usedPromptIds = _meetingsRepository.GetUsedPromptIds();

            var activePromptId = all
                .OrderByDescending(p => p.ConfirmDt)
                .ThenByDescending(p => p.Id)
                .Select(p => (int?)p.Id)
                .FirstOrDefault();

            return all
                .Where(p => p.Id == activePromptId || p.AuthorId == WebSession.User.Id)
                .OrderByDescending(p => p.CreateDt)
                .Select(p => new PromptInfo
                {
                    PromptId = p.Id,
                    Author = p.Author?.EMail ?? ("UserId " + p.AuthorId),
                    CreateDt = StringUtil.FormatDateTime(p.CreateDt),
                    Prompt = p.Prompt,
                    IsActive = p.Id == activePromptId,
                    CanDelete = p.Id != activePromptId && p.AuthorId == WebSession.User.Id && !usedPromptIds.Contains(p.Id)
                })
                .ToList();
        }

        public List<PromptInfo> ActivatePrompt(int promptId)
        {
            var prompt = GetPrompt(promptId);
            prompt.ConfirmDt = DateTime.Now;
            _db.Save(prompt);

            return GetPrompts();
        }

        public List<PromptInfo> CreatePrompt()
        {
            var prompt = _db.New<IMailConversationSummaryPrompt>();
            prompt.AuthorId = WebSession.User.Id;
            prompt.ConfirmDt = NonConfirmedPromptDt;
            prompt.CreateDt = DateTime.Now;
            prompt.Prompt = string.Empty;

            _db.Save(prompt);

            return GetPrompts();
        }

        public List<PromptInfo> DeletePrompt(int promptId)
        {
            var activePromptId = GetActivePromptId();
            if (promptId == activePromptId)
                throw new InvalidOperationException("Active prompt cannot be deleted");

            var prompt = GetPrompt(promptId);
            if (prompt.AuthorId != WebSession.User.Id)
                throw new InvalidOperationException("Only author can delete this prompt");

            if (_meetingsRepository.GetUsedPromptIds().Contains(promptId))
                throw new InvalidOperationException("Prompt used by existing summaries cannot be deleted");

            _db.Delete(prompt);
            return GetPrompts();
        }

        public List<PromptInfo> EditPrompt(PromptEditRequest request)
        {
            var id = request?.Id;
            var promptText = request?.PromptText;

            if (string.IsNullOrWhiteSpace(promptText))
                throw new ArgumentException("Prompt text must not be empty", nameof(promptText));

            var activePromptId = GetActivePromptId();

            IMailConversationSummaryPrompt prompt;
            if (id == null)
            {
                prompt = _db.New<IMailConversationSummaryPrompt>();
                prompt.AuthorId = WebSession.User.Id;
                prompt.ConfirmDt = NonConfirmedPromptDt;
            }
            else
            {
                prompt = GetPrompt(id.Value);

                if (prompt.Id == activePromptId)
                    throw new InvalidOperationException("Active prompt cannot be edited directly");

                if (prompt.AuthorId != WebSession.User.Id)
                    throw new InvalidOperationException("Only author can edit this prompt");
            }

            prompt.Prompt = promptText.Trim();
            prompt.CreateDt = DateTime.Now;

            _db.Save(prompt);

            return GetPrompts();
        }

        public List<int> PrepareConversationTestSet(int size)
        {
            size = Math.Max(1, Math.Min(size, 200));
            var candidateCount = Math.Min(1000, Math.Max(size * 10, size));

            return _meetingsRepository.GetConversationTestSet(candidateCount)
                .Where(c => _summarizer.CanUseAiSummary(c))
                .Take(size)
                .ToList();
        }

        public CustomerMeetingViewModel DoTestSummary(TestSummaryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var conversation = _meetingsRepository.GetMailConversation(request.ConversationId)
                .Ensure("Invalid conversationId");

            var sourcePrompt = string.IsNullOrWhiteSpace(request.PromptText)
                ? GetPrompt(request.PromptId).Prompt
                : request.PromptText;

            var summary = _summarizer.SummarizeConversation(request.ConversationId, sourcePrompt, false);

            var mapped = _mailConversationMeetingModelFactory.Create(new CustomerMeetingsRepository.MailConversationDto
            {
                Id = conversation.Id,
                ConversationEndDt = conversation.ConversationEndDt,
                Subject = conversation.Subject,
                Summary = summary.Summary
            });

            mapped.Title = conversation.Subject;
            mapped.Text = summary.Summary;

            return mapped;
        }

        public List<MailConversationMessageViewModel> GetMailConversationDetail(int conversationId)
        {
            EnsureUserRight(CrmUserRights.EmailConversationsFull);

            _meetingsRepository.GetMailConversation(conversationId)
                .Ensure("Invalid conversationId");

            return _meetingsRepository.GetMailConversationDetail(conversationId)
                .Select(m => new MailConversationMessageViewModel
                {
                    Id = m.Id,
                    Dt = StringUtil.FormatDateTime(m.InternalDt),
                    Sender = string.IsNullOrWhiteSpace(m.Sender) ? "(neznámý odesílatel)" : m.Sender,
                    Subject = string.IsNullOrWhiteSpace(m.Subject) ? "(bez předmětu)" : m.Subject,
                    Content = string.IsNullOrWhiteSpace(m.Content) ? "(bez textu)" : m.Content
                })
                .ToList();
        }

        public List<PromptInfo> ConfirmPrompt(int promptId, bool rebuildSummaries)
        {
            var prompt = GetPrompt(promptId);
            prompt.ConfirmDt = DateTime.Now;
            _db.Save(prompt);

            if (rebuildSummaries)
                _meetingsRepository.RebuildRecentMailConversationSummaries(DateTime.Now.AddDays(-365));

            return GetPrompts();
        }

        private IMailConversationSummaryPrompt GetPrompt(int promptId)
        {
            return _db.SelectFrom<IMailConversationSummaryPrompt>()
                .Where(p => p.Id == promptId)
                .Take(1)
                .Execute()
                .FirstOrDefault()
                .Ensure("Invalid promptId");
        }

        private int? GetActivePromptId()
        {
            return _db.SelectFrom<IMailConversationSummaryPrompt>()
                .OrderByDesc(p => p.ConfirmDt)
                .OrderByDesc(p => p.Id)
                .Take(1)
                .Execute()
                .Select(p => (int?)p.Id)
                .FirstOrDefault();
        }

        public class MailConversationMessageViewModel
        {
            public int Id { get; set; }
            public string Dt { get; set; }
            public string Sender { get; set; }
            public string Subject { get; set; }
            public string Content { get; set; }
        }

        public class PromptEditRequest
        {
            public int? Id { get; set; }
            public string PromptText { get; set; }
        }

        public class TestSummaryRequest
        {
            public int PromptId { get; set; }
            public int ConversationId { get; set; }
            public string PromptText { get; set; }
        }
    }
}
