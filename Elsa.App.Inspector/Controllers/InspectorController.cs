using System.Collections.Generic;
using System.Linq;
using Elsa.App.Inspector.Model;
using Elsa.App.Inspector.Repo;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;

namespace Elsa.App.Inspector.Controllers
{
    [Controller("inspector")]
    public class InspectorController : ElsaControllerBase
    {
        private readonly IInspectionsRepository m_inspectionsRepository;

        public InspectorController(IWebSession webSession, ILog log, IInspectionsRepository inspectionsRepository) : base(webSession, log)
        {
            m_inspectionsRepository = inspectionsRepository;
        }

        [DoNotLog]
        public List<IssuesSummaryItemModel> GetSummary()
        {
            return m_inspectionsRepository.GetActiveIssuesSummary();
        }

        public InspectionIssuesCollection GetIssues(int inspectionTypeId, int pageIndex)
        {
            var issues = m_inspectionsRepository.LoadIssues(inspectionTypeId, pageIndex, 10);

            var result = new InspectionIssuesCollection()
            {
                InspectionTypeId = inspectionTypeId,
                NextPageIndex = (issues.Count < 1) ? -1 : pageIndex+1
            };

            result.Issues.AddRange(issues.Select(i => new InspectionIssueViewModel(i)));

            return result;
        }

        public InspectionIssuesCollection LoadIssue(int issueId)
        {
            var issue = m_inspectionsRepository.LoadIssue(issueId, false);

            if (issue == null)
            {
                var type = m_inspectionsRepository.ResolveIssueTypeByIssueId(issueId);
                return InspectionIssuesCollection.CreateAsHidden(type, issueId);
            }

            var result = new InspectionIssuesCollection()
            {
                InspectionTypeId = issue.InspectionTypeId
            };

            result.Issues.Add(new InspectionIssueViewModel(issue));

            return result;
        }

        public InspectionIssuesCollection ReevalIssue(int issueId, string actionText)
        {
            using (var session = m_inspectionsRepository.OpenSession())
            {
                m_inspectionsRepository.RunInspectionAndCloseSession(session, issueId);
            }

            m_inspectionsRepository.LogUserAction(issueId, actionText);

            return LoadIssue(issueId);
        }

        public void PostponeIssue(int issueId, int days)
        {
            m_inspectionsRepository.PostponeIssue(issueId, days);
        }
    }
}
