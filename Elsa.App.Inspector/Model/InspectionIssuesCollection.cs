using System.Collections.Generic;

namespace Elsa.App.Inspector.Model
{
    public class InspectionIssuesCollection
    {
        public int InspectionTypeId { get; set; }

        public int? NextPageIndex { get; set; }
        
        public List<InspectionIssueViewModel> Issues { get; } = new List<InspectionIssueViewModel>();

        public static InspectionIssuesCollection CreateAsHidden(int inspectionTypeId, int issueId)
        {
            var res = new InspectionIssuesCollection()
            {
                InspectionTypeId = inspectionTypeId
            };

            res.Issues.Add(new InspectionIssueViewModel
            {
                IssueId = issueId,
                IsHidden = true
            });

            return res;
        }
    }
}
