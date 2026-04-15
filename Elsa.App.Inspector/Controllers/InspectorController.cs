using System.Collections.Generic;
using System.Linq;
using Elsa.App.Inspector.Model;
using Elsa.App.Inspector.Repo;
using Elsa.Apps.Reporting;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;

namespace Elsa.App.Inspector.Controllers
{
    [Controller("inspector")]
    public class InspectorController : ElsaControllerBase
    {
        private readonly IInspectionsRepository _inspectionsRepository;
        private readonly IUserRepository _userRepository;

        public InspectorController(IWebSession webSession, ILog log, IInspectionsRepository inspectionsRepository, IUserRepository userRepository) : base(webSession, log)
        {
            _inspectionsRepository = inspectionsRepository;
            _userRepository = userRepository;
        }

        [DoNotLog]
        public List<IssuesSummaryItemModel> GetSummary(int? userId)
        {
            if(!WebSession.HasUserRight(ReportingUserRights.InspectorApp))
                return new List<IssuesSummaryItemModel>(0);

            if (userId == null || (!WebSession.HasUserRight(ReportingUserRights.InspectorOtherUsers)))
            {
                userId = WebSession.User.Id;
            }

            var result =  _inspectionsRepository.GetActiveIssuesSummary(userId.Value);

            if (HasUserRight(ReportingUserRights.InspectorIssuesAssignment))
            {
                var matrix = _inspectionsRepository.GetResponsibilityMatrix();

                foreach (var sum in result)
                {
                    sum.ShowAssignments = true;                    
                    sum.AssignedUsersCount = matrix.Count(m => m.InspectionTypeId == sum.TypeId);
                }
            }

            return result;
        }

        public InspectionIssuesCollection GetIssues(int inspectionTypeId, int pageIndex)
        {
            EnsureUserRight(ReportingUserRights.InspectorApp);

            var issues = _inspectionsRepository.LoadIssues(inspectionTypeId, pageIndex, 10);

            var result = new InspectionIssuesCollection()
            {
                InspectionTypeId = inspectionTypeId,
                NextPageIndex = (issues.Count < 10) ? -1 : pageIndex+1
            };
                        
            result.Issues.AddRange(issues.Select(i => new InspectionIssueViewModel(i)));
                        
            return result;
        }

        public InspectionIssuesCollection LoadIssue(int issueId)
        {
            EnsureUserRight(ReportingUserRights.InspectorApp);

            var issue = _inspectionsRepository.LoadIssue(issueId, false);

            if (issue == null)
            {
                var type = _inspectionsRepository.ResolveIssueTypeByIssueId(issueId);
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
            EnsureUserRight(ReportingUserRights.InspectorActions);

            using (var session = _inspectionsRepository.OpenSession())
            {
                _inspectionsRepository.RunInspectionAndCloseSession(session, issueId);
            }

            _inspectionsRepository.LogUserAction(issueId, actionText);

            return LoadIssue(issueId);
        }

        public void PostponeIssue(int issueId, int days)
        {
            EnsureUserRight(ReportingUserRights.InspectorActions);

            _inspectionsRepository.PostponeIssue(issueId, days);
        }

        public List<UserIssuesCount> GetUsersIssuesCounts()
        {
            if (!WebSession.HasUserRight(ReportingUserRights.InspectorOtherUsers))
                return new List<UserIssuesCount>(0);            

            return _inspectionsRepository.GetUserIssuesCounts();
        }       

        public List<UserAssignmentInfo> GetAssignments(int inspectionTypeId)
        {
            EnsureUserRight(ReportingUserRights.InspectorIssuesAssignment);

            var assignments = new HashSet<int>(_inspectionsRepository
                .GetResponsibilityMatrix()
                .Where(m => m.InspectionTypeId == inspectionTypeId)
                .Select(m => m.ResponsibleUserId));

            var allUsers = _userRepository.GetAllUsers()
                .Where(u => u.EMail?.Contains("@") == true)
                .Where(u => u.LockDt == null);

            var result = new List<UserAssignmentInfo>();

            foreach (var u in allUsers.OrderBy(u => u.EMail))
                result.Add(new UserAssignmentInfo 
                {
                    UserId = u.Id,
                    Name = u.EMail,
                    Assigned = assignments.Contains(u.Id)
                });

            return result;
        }

        public List<UserAssignmentInfo> ChangeAssignment(int inspectionTypeId, int userId, bool assign)
        {
            EnsureUserRight(ReportingUserRights.InspectorIssuesAssignment);

            if (assign)
                _inspectionsRepository.SetResponsibleUser(inspectionTypeId, userId, null, 0);
            else
                _inspectionsRepository.RemoveResponsibleUser(inspectionTypeId, userId);

            return GetAssignments(inspectionTypeId);
        }        
    }
}
