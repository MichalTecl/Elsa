using Elsa.App.Crm.Entities;
using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CrmMeetings")]
    public class CrmMeetingsController : ElsaControllerBase
    {
        private readonly CustomerMeetingsRepository _meetingsRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDatabase _db;
        private readonly ICustomerRepository _customerRepository;

        public CrmMeetingsController(IWebSession webSession, ILog log, CustomerMeetingsRepository meetingsRepository, IUserRepository userRepository, IDatabase db, ICustomerRepository customerRepository) : base(webSession, log)
        {
            _meetingsRepository = meetingsRepository;
            _userRepository = userRepository;
            _db = db;
            _customerRepository = customerRepository;
        }

        public List<CustomerMeetingViewModel> GetMeetings(int customerId)
        {
            return MapMeetings(_meetingsRepository.GetMeetings(customerId)).ToList();
        }

        public List<CustomerMeetingViewModel> SetMeetingStatus(int meetingId, int statusTypeId) 
        {
            var meeting = _meetingsRepository.GetMeeting(meetingId).Ensure();

            meeting.StatusId = statusTypeId;

            _db.Save(meeting);

            return GetMeetings(meeting.CustomerId);
        }
                
        public CustomerMeetingViewModel GetMeetingTemplate(int customerId, int meetingCategoryId)
        {
            var category = _meetingsRepository
                .GetAllMeetingCategories()
                .FirstOrDefault(c => c.Id == meetingCategoryId)
                .Ensure("Invalid category id");

            var meeting = _db.New<IMeeting>();

            meeting.StartDt = DateTime.Now.AddDays(1);
            meeting.EndDt = DateTime.Now.AddDays(1);
            meeting.MeetingCategoryId = meetingCategoryId;
            meeting.StatusId = category.InitialStatusId;
            meeting.Title = category.Title;
            meeting.Text = category.Title;
            meeting.CustomerId = customerId;

            var mapped = MapMeeting(meeting);

            mapped.Participants.Add(new MeetingParticipantViewModel(WebSession.User));

            return mapped;
        }

        public List<MeetingParticipantViewModel> GetAllParticipants()
        {
            return _userRepository.GetAllUsers().Select(u => new MeetingParticipantViewModel(u)).ToList();
        }

        public List<CustomerMeetingViewModel> SaveMeeting(CustomerMeetingViewModel model)
        {
            var dbRecord = _meetingsRepository.GetMeetings(model.CustomerId).FirstOrDefault(r => r.Id == model.Id)
                ?? _db.New<IMeeting>(m =>
                {
                    m.AuthorId = WebSession.User.Id;
                });

            using (var tx = _db.OpenTransaction())
            {

                dbRecord.StartDt = StringUtil.ParseUiInputDateTime(model.StartDt);
                dbRecord.EndDt = StringUtil.ParseUiInputDateTime(model.EndDt);
                dbRecord.MeetingCategoryId = model.CategoryId;
                dbRecord.Title = model.Title.TrimAndValidateNonEmpty(() => "Název schůzky nesmí být prázdný");
                dbRecord.Text = model.Text?.Trim() ?? string.Empty;
                dbRecord.MeetingCategoryId = model.CategoryId;
                dbRecord.StatusId = model.StatusTypeId;
                dbRecord.MeetingCategoryId = model.CategoryId;
                dbRecord.CustomerId = model.CustomerId;

                _db.Save(dbRecord);

                // Add missing participants
                foreach (var uiParticipant in model.Participants)
                    if (!dbRecord.Participants.Any(dbParticipant => dbParticipant.ParticipantId == uiParticipant.UserId))
                    {
                        var bridge = _db.New<IMeetingParticipant>();
                        bridge.ParticipantId = uiParticipant.UserId;
                        bridge.MeetingId = dbRecord.Id;

                        _db.Save(bridge);
                    }

                // Remove invalid participants
                foreach (var dbParticipant in dbRecord.Participants)
                    if (!model.Participants.Any(uiParticipant => uiParticipant.UserId == dbParticipant.ParticipantId))
                    {
                        _db.Delete(dbParticipant);
                    }

                tx.Commit();
            }

            return GetMeetings(model.CustomerId);
        }

        public MeetingsOverview GetMyMeetingsOverview()
        {
            var types = GetMeetingTimeDeciders().ToDictionary(d => d, d => 0);

            var meetings = _meetingsRepository
                .GetParticipantMeetings(WebSession.User.Id, DateTime.Now.AddDays(-30), DateTime.Now.AddDays(16))
                .Where(m => m.Status.ActionExpected).ToList();

            var allKeys = types.Keys.ToList();

            foreach (var m in meetings)
            {
                if (!m.Status.ActionExpected)
                    continue;

                var meetDt = m.StartDt;

                foreach (var k in allKeys)
                {
                    if (k.from <= meetDt && k.to >= meetDt)
                    {
                        types[k] = types[k] + 1;
                    }
                }
            }

            var text = string.Join(", ", types
                .Where(kv => kv.Value > 0)
                .OrderBy(kv => kv.Key.order)
                .Select(kv => $"{kv.Value} {kv.Key.text}"));

            return new MeetingsOverview
            {
                Text = text,
                IsWarning = false
            };
        }
        
        public FileResult GetIcsFile()
        {
            var meetings = _meetingsRepository.GetParticipantMeetings(WebSession.User.Id, DateTime.Now.AddDays(-30), DateTime.Now.AddDays(32));

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:https://robotelsa.com/");
            sb.AppendLine();

            foreach(var meeting in meetings)
            {
                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine($"UID:crmevent{meeting.Id}@robotelsa.com");

                if (!string.IsNullOrWhiteSpace(meeting.Title))
                    sb.AppendLine($"SUMMARY:{meeting.Title.Replace("\r","").Replace("\n","").Trim()}");

                if (!string.IsNullOrWhiteSpace(meeting.Text))
                    sb.AppendLine($"DESCRIPTION:{meeting.Text.Replace("\r", "").Replace("\n", "").Trim()}");

                sb.Append("DTSTAMP:").AppendLine(DateTime.Now.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"));
                sb.Append("DTSTART:").AppendLine(meeting.StartDt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"));
                sb.Append("DTSEND:").AppendLine(meeting.EndDt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"));

                sb.AppendLine("END:VEVENT");
                sb.AppendLine();
            }

            sb.AppendLine("END:VCALENDAR");

            return new FileResult("crmElsa.ics", Encoding.UTF8.GetBytes(sb.ToString()), "text/calendar; charset=utf-8");
        }

        public List<CustomerMeetingViewModel> GetMyMeetings()
        {
            var meetings = _meetingsRepository.GetParticipantMeetings(WebSession.User.Id, DateTime.Now.AddDays(-30), DateTime.Now.AddDays(32));

            return MapMeetings(meetings.Where(m => m.Status.ActionExpected).OrderBy(m => m.StartDt)).ToList();
        }

        private static IEnumerable<(int order, DateTime from, DateTime to, string text, string grpClass)> GetMeetingTimeDeciders()
        {
            var now = DateTime.Now;
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);
            var tomorrowStart = todayEnd;
            var tomorrowEnd = tomorrowStart.AddDays(1);
            var startOfThisWeek = todayStart.AddDays(-(int)todayStart.DayOfWeek + (int)DayOfWeek.Monday);
            var startOfNextWeek = startOfThisWeek.AddDays(7);
            var endOfNextWeek = startOfNextWeek.AddDays(7);

            int order = 0;

            (int order, DateTime from, DateTime to, string text, string grpClass) Def(DateTime from, DateTime to, string label, string grpClass)
            {
                return (order++, from, to, label, grpClass);
            }

            yield return Def(todayStart, todayEnd, "dnes", "mtGrpToday");
            yield return Def(tomorrowStart, tomorrowEnd, "zítra", "mtGrpTomorrow");
            yield return Def(tomorrowEnd, startOfNextWeek, "tento týden", "mtGrpThisWeek");
            yield return Def(startOfNextWeek, endOfNextWeek, "příští týden", "mtGrpNextWeek");
            yield return Def(DateTime.MinValue, todayStart, "neuzavřených", "mtGrpPassed");
        }

        private T MapStatus<T>(IEnumerable<IMeetingStatus> index, T vm) where T : StatusVmBase
        {
            var st = index.FirstOrDefault(s => s.Id == vm.StatusTypeId);

            if (st == null)
                return vm;

            vm.StatusTypeName = st.Title;
            vm.StatusTypeColor = st.ColorHex;
            vm.StatusTypeIconClass = st.IconClass;

            return vm;
        }

        private IEnumerable<CustomerMeetingViewModel> MapMeetings(IEnumerable<IMeeting> records)
        {
            var deciders = GetMeetingTimeDeciders().ToList();

            var userIndex = _userRepository.GetUserIndex();
            var meetingStatusIndex = _meetingsRepository.GetMeetingStatusTypes();
            var actionsIndex = _meetingsRepository.GetMeetingStatusActions(null).OrderBy(a => a.SortOrder).ToList();
            var categoryIndex = _meetingsRepository.GetAllMeetingCategories();
            var customerIndex = _customerRepository.GetDistributorNameIndex();

            foreach (var record in records)
            {
                var category = categoryIndex.First(c => c.Id == record.MeetingCategoryId);
                var decider = deciders.FirstOrDefault(d => d.from <= record.StartDt && d.to > record.EndDt);

                var model = new CustomerMeetingViewModel
                {
                    Id = record.Id,
                    Day = StringUtil.FormatDate_DayNameDdMm(record.StartDt),
                    Time = StringUtil.FormatTimeHhMm(record.StartDt),
                    StartDt = StringUtil.FormatDateTimeForUiInput(record.StartDt),
                    EndDt = StringUtil.FormatDateTimeForUiInput(record.EndDt),
                    Title = record.Title,
                    Text = record.Text,
                    Author = userIndex.Get(record.AuthorId, null)?.EMail,
                    StatusTypeId = record.StatusId,
                    CategoryId = record.MeetingCategoryId,
                    CategoryName = category.Title,
                    CategoryIconClass = category.IconClass,
                    CustomerId = record.CustomerId,
                    ExpectedDurationMinutes = category.ExpectedDurationMinutes,
                    CustomerName = customerIndex.TryGetValue(record.CustomerId, out var customerName) ? customerName : "?",
                    TimeGroup = StringUtil.Capitalize(decider.text),
                    TimeGroupClass = decider.grpClass
                };
                                
                MapStatus(meetingStatusIndex, model);

                // Participants list should be ordered as: 1. viewing user, 2. organizer, 3. others
                foreach (var p in record.Participants.OrderBy(p => p.Id == record.AuthorId ? 2 : p.Id == WebSession.User.Id ? 1 : 3))
                    model.Participants.Add(new MeetingParticipantViewModel(userIndex.Get(p.ParticipantId, null)));

                foreach (var action in actionsIndex.Where(a => a.CurrentStatusTypeId == model.StatusTypeId))
                    model.Actions.Add(MapStatus(meetingStatusIndex, new MeetingStatusActionViewModel
                    {
                        Id = action.Id,
                        Title = action.Title,
                        StatusTypeId = action.NextStatusTypeId
                    }));

                yield return model;
            }            
        }

        private CustomerMeetingViewModel MapMeeting(IMeeting meeting)
        {
            return MapMeetings(new[] { meeting }).Single();
        }

        




    }
}
