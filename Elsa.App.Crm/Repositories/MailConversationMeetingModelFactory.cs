using Elsa.App.Crm.Model;
using Elsa.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.App.Crm.Repositories
{
    public class MailConversationMeetingModelFactory
    {
        private readonly CustomerMeetingsRepository _meetingsRepository;

        public MailConversationMeetingModelFactory(CustomerMeetingsRepository meetingsRepository)
        {
            _meetingsRepository = meetingsRepository;
        }

        public CustomerMeetingViewModel Create(CustomerMeetingsRepository.MailConversationDto conversation)
        {
            var conversationDt = conversation?.ConversationEndDt ?? DateTime.MinValue;
            var decider = GetMeetingTimeDeciders().FirstOrDefault(d => d.from <= conversationDt && d.to > conversationDt);
            var meetingStatusIndex = _meetingsRepository.GetMeetingStatusTypes();
            var categoryIndex = _meetingsRepository.GetAllMeetingCategories();
            var emailCategory = categoryIndex.FirstOrDefault(c => string.Equals(c.Title, "E-Mail", StringComparison.OrdinalIgnoreCase));
            var pastStatus = meetingStatusIndex.FirstOrDefault(s => string.Equals(s.Title, "Proběhlo", StringComparison.OrdinalIgnoreCase))
                ?? meetingStatusIndex.FirstOrDefault(s => !s.ActionExpected && s.MeansCancelled != true)
                ?? meetingStatusIndex.FirstOrDefault(s => !s.ActionExpected);

            var model = new CustomerMeetingViewModel
            {
                Id = conversation.Id * -1,
                MailConversationId = conversation.Id,
                Day = StringUtil.FormatDate_DayNameDdMm(conversationDt),
                Time = StringUtil.FormatTimeHhMm(conversationDt),
                StartDt = StringUtil.FormatDateTimeForUiInput(conversationDt),
                EndDt = StringUtil.FormatDateTimeForUiInput(conversationDt),
                Title = conversation.Subject,
                Text = conversation.Summary ?? "Shrnutí zatím nebylo vytvořeno...",
                StatusTypeId = pastStatus?.Id ?? 0,
                CategoryId = emailCategory?.Id ?? 0,
                CategoryName = emailCategory?.Title ?? "E-Mail",
                CategoryIconClass = emailCategory?.IconClass ?? "fas fa-envelope",
                ExpectedDurationMinutes = emailCategory?.ExpectedDurationMinutes ?? 1,
                TimeGroup = StringUtil.Capitalize(decider.text),
                TimeGroupClass = decider.grpClass
            };

            if (pastStatus != null)
            {
                model.StatusTypeName = pastStatus.Title;
                model.StatusTypeColor = pastStatus.ColorHex;
                model.StatusTypeIconClass = pastStatus.IconClass;
            }

            model.StatusTypeColor = "#7A8A99";

            return model;
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
    }
}
