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
                .Where(m => m.Id == meetingId)
                .Where(m => m.Customer.ProjectId == _session.Project.Id)
                .Take(1)
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

        public List<IMeeting> GetParticipantMeetings(int participantId, DateTime fromDt, DateTime toDt)
        {
            var now = DateTime.Now;

            return _database.SelectFrom<IMeeting>()
                .Join(m => m.Customer)
                .Join(m => m.Participants)
                .Join(m => m.Status)
                .Where(m => m.Participants.Each().ParticipantId == participantId)
                .Where(m => m.StartDt >= fromDt && m.StartDt <= toDt)
                .OrderByDesc(m => m.StartDt)
                .Execute()
                .ToList();
        }

        public List<MailConversationDto> GetMailConversations(int customerId)
        {
            return _database.Sql()
                .Call("GetCustomerMailConversations")
                .WithParam("@customerId", customerId)
                .AutoMap<MailConversationDto>();
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
