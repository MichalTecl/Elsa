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

            var usedPromptIds = GetUsedPromptIds();

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

            if (GetUsedPromptIds().Contains(promptId))
                throw new InvalidOperationException("Prompt used by existing summaries cannot be deleted");

            _db.Delete(prompt);
            return GetPrompts();
        }

        public List<PromptInfo> EditPrompt(int? id, string promptText)
        {
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
            var result = new List<int>();

            _db.Sql().Execute(@"
WITH candidates AS
(
    SELECT ade.CustomerId,
           mc.Id ConversationId,
           COUNT(DISTINCT mmr.Id) MessageCount,
           SUM(LEN(ISNULL(mfc.Content, ''))) ContentLength,
            ROW_NUMBER() OVER (PARTITION BY ade.CustomerId ORDER BY NEWID()) CustomerSpreadOrder
      FROM dbo.MailConversation mc
      JOIN dbo.MailConversationSummary mcs ON (mc.SummaryId = mcs.Id)
      JOIN dbo.MailMessageReference mmr ON (mmr.ConversationId = mc.Id)
      JOIN dbo.MailMessageFullContent mfc ON (mfc.Id = mmr.FullContentId)
      JOIN dbo.MailMessageReferenceParticipant mmrp ON (mmrp.MailMessageReferenceId = mmr.Id)
      JOIN dbo.MessageParticipantAddress mpa ON (mpa.Id = mmrp.ParticipantAddressId)
      JOIN dbo.vwAllDistributorEmails ade ON (ade.Email = mpa.Email)
     WHERE mcs.PromptId IS NOT NULL
     GROUP BY ade.CustomerId, mc.Id
),
collapsed AS
(
    SELECT c.ConversationId,
           MIN(c.CustomerSpreadOrder) CustomerSpreadOrder,
           MAX(c.MessageCount) MessageCount,
           MAX(c.ContentLength) ContentLength
      FROM candidates c
     GROUP BY c.ConversationId
)
SELECT TOP (@size) c.ConversationId
  FROM collapsed c
 ORDER BY c.CustomerSpreadOrder,
          CASE
              WHEN c.MessageCount <= 2 THEN 1
              WHEN c.MessageCount <= 5 THEN 2
              ELSE 3
          END,
          CASE
              WHEN c.ContentLength <= 1000 THEN 1
              WHEN c.ContentLength <= 5000 THEN 2
              ELSE 3
          END,
          NEWID()")
                .WithParam("@size", size)
                .ReadRows<int>(result.Add);

            return result;
        }

        public CustomerMeetingViewModel DoTestSummary(int promptId, int conversationId)
        {
            var prompt = GetPrompt(promptId);
            var conversation = _meetingsRepository.GetMailConversation(conversationId)
                .Ensure("Invalid conversationId");

            var summary = _summarizer.SummarizeConversation(conversationId, prompt.Prompt, false);

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

        public List<PromptInfo> ConfirmPrompt(int promptId, bool rebuildSummaries)
        {
            var prompt = GetPrompt(promptId);
            prompt.ConfirmDt = DateTime.Now;
            _db.Save(prompt);

            if (rebuildSummaries)
                RebuildRecentSummaries();

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

        private HashSet<int> GetUsedPromptIds()
        {
            var result = new HashSet<int>();

            _db.Sql()
                .Execute("SELECT DISTINCT PromptId FROM dbo.MailConversationSummary WHERE PromptId IS NOT NULL")
                .ReadRows<int>(i => result.Add(i));

            return result;
        }

        private void RebuildRecentSummaries()
        {
            var cutoffDt = DateTime.Now.AddDays(-365);

            _db.Sql().Execute(@"
IF OBJECT_ID('tempdb..#SummariesToDrop') IS NOT NULL
    DROP TABLE #SummariesToDrop;

CREATE TABLE #SummariesToDrop
(
    ConversationId INT NOT NULL PRIMARY KEY,
    SummaryId INT NOT NULL
);

INSERT INTO #SummariesToDrop (ConversationId, SummaryId)
SELECT DISTINCT mc.Id, mc.SummaryId
  FROM dbo.MailConversation mc
  JOIN dbo.MailMessageReference mmr ON (mmr.ConversationId = mc.Id)
 WHERE mc.SummaryId IS NOT NULL
   AND mmr.InternalDt >= @cutoffDt;

UPDATE mc
   SET mc.SummaryId = NULL
  FROM dbo.MailConversation mc
  JOIN #SummariesToDrop d ON (d.ConversationId = mc.Id);

DELETE mcs
  FROM dbo.MailConversationSummary mcs
  JOIN #SummariesToDrop d ON (d.SummaryId = mcs.Id);")
                .WithParam("@cutoffDt", cutoffDt);
        }
    }
}
