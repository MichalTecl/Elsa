using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class MeetingColumnsProvider : IDynamicColumnProvider
    {
        private static readonly ColumnInfo _pastMeetingColumn = new ColumnInfo(91, "PastMeeting", "Minulý kontakt");
        private static readonly ColumnInfo _nextMeetingColumn = new ColumnInfo(92, "NextMeeting", "Příští kontakt");

        private static readonly Dictionary<string, (ColumnInfo, Func<ClosestMeetingsInfoModel, MeetingInfoModel>, DateTime)> _cols = new Dictionary<string, (ColumnInfo, Func<ClosestMeetingsInfoModel, MeetingInfoModel>, DateTime)> {
            { _pastMeetingColumn.Id, (_pastMeetingColumn, m => m.PastMeeting, DateTime.MinValue) },
            { _nextMeetingColumn.Id, (_nextMeetingColumn, m => m.FutureMeeting, DateTime.MaxValue) },
        };
        

        private readonly CustomerMeetingsRepository _customerMeetingsRepository;

        public MeetingColumnsProvider(CustomerMeetingsRepository customerMeetingsRepository)
        {
            _customerMeetingsRepository = customerMeetingsRepository;
        }

        public IReadOnlyCollection<ColumnInfo> GetAvailableColumns()
        {
            return _cols.Values.Select(v => v.Item1).ToList();
        }

        public void Populate(string columnId, List<DistributorGridRowModel> rows, bool? sortDescending)
        {
            var data = _customerMeetingsRepository.GetMeetingsInfo();

            var colDef = _cols[columnId];

            foreach (var row in rows) 
            {
                MeetingInfoModel meeting;

                if (data.TryGetValue(row.Id, out var meetingInfo))
                    meeting = colDef.Item2(meetingInfo);
                else
                    meeting = new MeetingInfoModel() { Dt = colDef.Item3 };

                if(meeting.Dt == null)
                    meeting.Dt = colDef.Item3;

                row.DynamicColumns[columnId] = meeting;
            }

            if (sortDescending != null)
            {
                rows.Sort(DistributorGridRowModel.GetComparer(r => ((MeetingInfoModel)r.DynamicColumns[columnId]).Dt, sortDescending.Value));
            }
        }

        public void RenderCell(string columnId, Func<string, string> mapPath, StringBuilder sb)
        {
            var columnAccess = $"DynamicColumns.{columnId}.";

            sb.Append($"<div class=\"cell10 digrMeetingColumnCellCont\">");
            sb.Append($" <div class=\"digrMeetingInlinePreview\">");
            sb.Append($"  <i data-bind=\"class:{columnAccess}Icon\"></i>");
            sb.Append($"  <div class=\"digrMeetingColumnCellDt\" data-bind=\"text:{columnAccess}DtF\"></div>");
            sb.Append($"  <div class=\"digrMeetingColumnCellText\" data-bind=\"text:{columnAccess}Text\"></div>");
            sb.Append($" </div>");
            sb.Append($" <div class=\"digrMeetingFullText\" data-bind=\"text:{columnAccess}Text\"></div>");
            sb.Append($"</div>");
        }

        public void RenderHead(string columnId, Func<string, string> mapPath, StringBuilder sb)
        {
            ColumnHeadControlLoader.Render(columnId, _cols[columnId].Item1.Title, "cell10", true, mapPath, sb);
        }
    }
}
