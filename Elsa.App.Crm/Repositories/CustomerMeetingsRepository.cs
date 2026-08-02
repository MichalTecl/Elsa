using Elsa.App.Crm.Entities;
using Elsa.App.Crm.Model;
using Elsa.Common.Caching;
using Elsa.Common.Data;
using Elsa.Common.Interfaces;
using Elsa.Jobs.CrmMailPull.Entities;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories
{
    public class CustomerMeetingsRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ICache _cache;

        private readonly AutoRepo<IMeetingCategory> _meetingCategoryRepo;
        private readonly AutoRepo<IMeetingStatus> _meetingStautsTypeRepo;
        private readonly AutoRepo<IMeetingStatusAction> _meetingStatusActionRepo;
        
        private readonly DistributorsRepository _distributorsRepo;

        public CustomerMeetingsRepository(IDatabase database, ISession session, ICache cache, DistributorsRepository distributorsRepo)
        {
            _database = database;
            _session = session;
            _cache = cache;

            _meetingCategoryRepo = new AutoRepo<IMeetingCategory>(session, database, cache, selectQueryModifier: (db, q) => q.OrderBy(i => i.Title));
            _meetingStautsTypeRepo = new AutoRepo<IMeetingStatus>(session, database, cache, selectQueryModifier: (db, q) => q.OrderBy(i => i.Title));
            _meetingStatusActionRepo = new AutoRepo<IMeetingStatusAction>(session, database, cache, selectQueryModifier: (db, q) => q.OrderBy(i => i.SortOrder));
            _distributorsRepo = distributorsRepo;
        }

        public IReadOnlyCollection<IMeetingCategory> GetAllMeetingCategories()
        {
            return _meetingCategoryRepo.GetAll();
        }

        public IReadOnlyCollection<IMeetingStatus> GetMeetingStatusTypes()
        {
            return _meetingStautsTypeRepo.GetAll();
        }

        public List<IMeetingStatusAction> GetMeetingStatusActions(int? currentStatusId)
        {
            return _meetingStatusActionRepo.GetAll().Where(ms => currentStatusId == null || currentStatusId == ms.CurrentStatusTypeId).ToList();
        }

        public List<IMeeting> GetMeetings(int customerId)
        {
            return _database.SelectFrom<IMeeting>()
                 .Join(m => m.Customer)
                 .Join(m => m.Participants)
                .Where(m => m.CustomerId == customerId)
                .Where(m => m.Customer.ProjectId == _session.Project.Id)
                .OrderByDesc(m => m.StartDt)
                .Execute()
                .ToList();
        }

        public IMeeting GetMeeting(int meetingId)
        {
            return _database.SelectFrom<IMeeting>()
                .Join(m => m.Customer)
                .Join(m => m.Participants)
                .Join(m => m.Status)
                .Where(m => m.Id == meetingId)
                .Where(m => m.Customer.ProjectId == _session.Project.Id)
                .Execute()
                .FirstOrDefault();
        }

        public IReadOnlyDictionary<int, ClosestMeetingsInfoModel> GetMeetingsInfo()
        {
            // there could be multiple meetings at same time
            var result = new Dictionary<int, ClosestMeetingsInfoModel>();

            foreach (var m in _database.Sql()
                .Call("CrmGridGetMeetingsColumns")
                .AutoMap<ClosestMeetingsInfoModel>())
            {
                result[m.CustomerId] = m;
            }
               
            return result;
        }

        public List<IMeeting> GetParticipantMeetings(int participantId, DateTime? fromDt = null, DateTime? toDt = null)
        {
            var now = DateTime.Now;
            var effectiveFromDt = fromDt ?? now.AddYears(-10);
            var effectiveToDt = toDt ?? now.AddYears(10);

            // Filtering by a joined participant also limits the hydrated Participants collection.
            // Resolve matching IDs first, then reload each meeting with its complete collection.
            var meetingIds = _database.SelectFrom<IMeeting>()
                .Join(m => m.Customer)
                .Join(m => m.Participants)
                .Where(m => m.Participants.Each().ParticipantId == participantId)
                .Where(m => m.Customer.ProjectId == _session.Project.Id)
                .Where(m => m.StartDt >= effectiveFromDt && m.StartDt <= effectiveToDt)
                .Execute()
                .Select(m => m.Id)
                .Distinct()
                .ToList();

            return meetingIds
                .Select(GetMeeting)
                .Where(m => m != null)
                .OrderByDescending(m => m.StartDt)
                .ToList();
        }

        public List<IMeeting> GetActionExpectedParticipantMeetings(int participantId)
        {
            var now = DateTime.Now;
            var fromDt = now.AddYears(-10);
            var toDt = now.AddYears(10);

            // Keep the participant join unfiltered so the ORM hydrates the complete collection.
            // The project has few users, so filtering the materialized meetings in .NET is cheaper
            // than issuing a separate query for every matching meeting.
            var meetings = _database.SelectFrom<IMeeting>()
                .Join(m => m.Customer)
                .Join(m => m.Participants)
                .Join(m => m.Status)
                .Where(m => m.Customer.ProjectId == _session.Project.Id)
                .Where(m => m.Status.ActionExpected == true)
                .Where(m => m.StartDt >= fromDt && m.StartDt <= toDt)
                .OrderByDesc(m => m.StartDt)
                .Execute()
                .ToList();

            return meetings
                .Where(m => m.Participants.Any(p => p.ParticipantId == participantId))
                .ToList();
        }

        public ParticipantMeetingsOverview GetParticipantMeetingsOverview(int participantId)
        {
            var now = DateTime.Now;

            return _database.Sql()
                .Execute(@"
SELECT COUNT(DISTINCT m.Id) MeetingCount,
       CAST(ISNULL(MAX(CASE WHEN m.StartDt < @now THEN 1 ELSE 0 END), 0) AS bit) IsWarning
  FROM dbo.Meeting m
  JOIN dbo.MeetingParticipant mp ON (mp.MeetingId = m.Id)
  JOIN dbo.MeetingStatus ms ON (ms.Id = m.StatusId)
  JOIN dbo.Customer c ON (c.Id = m.CustomerId)
 WHERE mp.ParticipantId = @participantId
   AND c.ProjectId = @projectId
   AND ms.ActionExpected = 1
   AND m.StartDt >= @fromDt
   AND m.StartDt <= @toDt")
                .WithParam("@participantId", participantId)
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@fromDt", now.AddYears(-10))
                .WithParam("@toDt", now.AddYears(10))
                .WithParam("@now", now)
                .AutoMap<ParticipantMeetingsOverview>()
                .Single();
        }

        public List<MailConversationDto> GetMailConversations(int customerId)
        {
            return _database.Sql()
                .Call("GetCustomerMailConversations")
                .WithParam("@customerId", customerId)
                .AutoMap<MailConversationDto>();
        }

        public MailConversationDto GetMailConversation(int conversationId)
        {
            return _database.SelectFrom<IMailConversation>()
                .Join(c => c.Summary)
                .Where(c => c.Id == conversationId)
                .Take(1)
                .Execute()
                .Select(c => new MailConversationDto
                {
                    Id = c.Id,
                    ConversationEndDt = c.ConversationEndDt,
                    Subject = c.Summary?.SubjectSummary ?? c.Hint,
                    Summary = c.Summary?.Summary
                })
                .FirstOrDefault();
        }

        public List<int> GetConversationTestSet(int size)
        {
            size = Math.Max(1, Math.Min(size, 1000));
            var result = new List<int>();

            _database.Sql().Execute(@"
WITH candidates AS
(
    SELECT ade.CustomerId,
           mc.Id ConversationId,
           COUNT(DISTINCT mmr.Id) MessageCount,
           SUM(LEN(ISNULL(mfc.Content, ''))) ContentLength,
           CASE WHEN mc.SummaryId IS NULL THEN 1 ELSE 0 END NeedsSummary,
           ROW_NUMBER() OVER (
               PARTITION BY ade.CustomerId, CASE WHEN mc.SummaryId IS NULL THEN 1 ELSE 0 END
               ORDER BY NEWID()) CustomerSpreadOrder
      FROM dbo.MailConversation mc
      JOIN dbo.MailMessageReference mmr ON (mmr.ConversationId = mc.Id)
      JOIN dbo.MailMessageFullContent mfc ON (mfc.Id = mmr.FullContentId)
      JOIN dbo.MailMessageReferenceParticipant mmrp ON (mmrp.MailMessageReferenceId = mmr.Id)
      JOIN dbo.MessageParticipantAddress mpa ON (mpa.Id = mmrp.ParticipantAddressId)
      JOIN dbo.vwAllDistributorEmails ade ON (ade.Email = mpa.Email)
     GROUP BY ade.CustomerId, mc.Id, CASE WHEN mc.SummaryId IS NULL THEN 1 ELSE 0 END
),
collapsed AS
(
    SELECT c.ConversationId,
           MIN(c.CustomerSpreadOrder) CustomerSpreadOrder,
           MAX(c.MessageCount) MessageCount,
           MAX(c.ContentLength) ContentLength,
           MIN(c.NeedsSummary) NeedsSummary
      FROM candidates c
     GROUP BY c.ConversationId
)
SELECT TOP (@size) c.ConversationId
  FROM collapsed c
 ORDER BY c.NeedsSummary,
          c.CustomerSpreadOrder,
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

        public HashSet<int> GetUsedPromptIds()
        {
            var result = new HashSet<int>();

            _database.Sql()
                .Execute("SELECT DISTINCT PromptId FROM dbo.MailConversationSummary WHERE PromptId IS NOT NULL")
                .ReadRows<int>(i => result.Add(i));

            return result;
        }

        public void RebuildRecentMailConversationSummaries(DateTime cutoffDt)
        {
            _database.Sql().Execute(@"
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

        public List<MailConversationMessageDto> GetMailConversationDetail(int conversationId)
        {
            return _database.SelectFrom<IMailMessageReference>()
                .Join(m => m.FullContent)
                .Where(m => m.ConversationId == conversationId)
                .Where(m => m.FullContentId != null)
                .OrderBy(m => m.InternalDt)
                .Execute()
                .Select(m => new MailConversationMessageDto
                {
                    Id = m.Id,
                    InternalDt = m.InternalDt,
                    Sender = m.FullContent?.Sender,
                    Subject = m.FullContent?.Subject,
                    Content = m.FullContent?.Content
                })
                .ToList();
        }

        public class MailConversationDto
        {
            public int Id { get; set; }
            public DateTime? ConversationEndDt { get; set; }
            public string Subject { get; set; }
            public string Summary { get; set; }
            public int MessageCount { get; set; }
        }

        public class ParticipantMeetingsOverview
        {
            public int MeetingCount { get; set; }
            public bool IsWarning { get; set; }
        }

        public class MailConversationMessageDto
        {
            public int Id { get; set; }
            public DateTime InternalDt { get; set; }
            public string Sender { get; set; }
            public string Subject { get; set; }
            public string Content { get; set; }
        }
    }
}
