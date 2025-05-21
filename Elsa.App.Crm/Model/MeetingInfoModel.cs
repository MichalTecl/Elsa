using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class MeetingInfoModel
    {
        public static readonly MeetingInfoModel EmptyFuture = new MeetingInfoModel() { };

        public string Icon { get; set; }
        public string Text { get; set; }
        public string DtF { get; set; }
        public DateTime? Dt { get; set; }

        public bool MeetingExists => Dt != null;
    }

    public class ClosestMeetingsInfoModel
    {
        public int CustomerId { get; set; }

        public MeetingInfoModel PastMeeting { get; } = new MeetingInfoModel();
        public MeetingInfoModel FutureMeeting { get; } = new MeetingInfoModel();

        public string FutureMeetingIcon
        {
            get => FutureMeeting.Icon;
            set => FutureMeeting.Icon = value;
        }

        public string FutureMeetingDtF
        {
            get => FutureMeeting.DtF;
            set => FutureMeeting.DtF = value;
        }

        public string FutureMeetingText
        {
            get => FutureMeeting.Text;
            set => FutureMeeting.Text = value;
        }

        public DateTime? FutureMeetingDt
        {
            get => FutureMeeting.Dt;
            set => FutureMeeting.Dt = value;
        }

        // Past meeting properties
        public string PastMeetingIcon
        {
            get => PastMeeting.Icon;
            set => PastMeeting.Icon = value;
        }

        public string PastMeetingDtF
        {
            get => PastMeeting.DtF;
            set => PastMeeting.DtF = value;
        }

        public string PastMeetingText
        {
            get => PastMeeting.Text;
            set => PastMeeting.Text = value;
        }

        public DateTime? PastMeetingDt
        {
            get => PastMeeting.Dt;
            set => PastMeeting.Dt = value;
        }
    }
}
