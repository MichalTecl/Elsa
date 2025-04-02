using Elsa.App.Crm.Entities;
using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CrmMeetings")]
    public class CrmMeetingsController : ElsaControllerBase
    {
        private readonly CustomerMeetingsRepository _meetingsRepository;
        private readonly IUserRepository _userRepository;

        public CrmMeetingsController(IWebSession webSession, ILog log, CustomerMeetingsRepository meetingsRepository, IUserRepository userRepository) : base(webSession, log)
        {
            _meetingsRepository = meetingsRepository;
            _userRepository = userRepository;
        }

        public List<CustomerMeetingViewModel> GetMeetings(int customerId)
        {
            var userIndex = _userRepository.GetUserIndex();
            var meetingStatusIndex = _meetingsRepository.GetMeetingStatusTypes();
            var actionsIndex = _meetingsRepository.GetMeetingStatusActions(null).OrderBy(a => a.SortOrder).ToList();

            var records = _meetingsRepository.GetMeetings(customerId);

            var result = new List<CustomerMeetingViewModel>(records.Count);

            foreach (var record in records) 
            {
                var model = new CustomerMeetingViewModel
                {
                    Id = record.Id,
                    StartDt = StringUtil.FormatDate(record.StartDt),
                    EndDt = StringUtil.FormatDate(record.EndDt, null),
                    Title = record.Title,
                    Text = record.Text,
                    Author = userIndex.Get(record.AuthorId, null)?.EMail,
                    StatusTypeId = record.MeetingStatuses.OrderByDescending(ms => ms.SetDt).FirstOrDefault()?.StatusTypeId ?? -1
                };

                result.Add(model);

                MapStatus(meetingStatusIndex, model);

                // Participants list should be ordered as: 1. viewing user, 2. organizer, 3. others
                foreach(var p in record.Participants.OrderBy(p => p.Id == record.AuthorId ? 2 : p.Id == WebSession.User.Id ? 1 : 3))
                    model.Participants.Add(userIndex.Get(p.ParticipantId, null));
                
                foreach(var action in actionsIndex.Where(a => a.CurrentStatusTypeId == model.StatusTypeId))
                    model.Actions.Add(MapStatus(meetingStatusIndex, new MeetingStatusActionViewModel
                    {
                        Id = action.Id,
                        Title = action.Title,
                        RequiresNote = action.RequiresNote,
                        StatusTypeId = action.NextStatusTypeId
                    }));
            }

            return result;
        }

        private T MapStatus<T>(List<IMeetingStatusType> index, T vm) where T : StatusVmBase
        {
            var st = index.FirstOrDefault(s => s.Id == vm.StatusTypeId);

            if (st == null)
                return vm;

            vm.StatusTypeName = st.Title;
            vm.StatusTypeColor = st.ColorHex;
            vm.StatusTypeIconClass = st.IconClass;

            return vm;
        }
    }
}
