using System;
using System.Collections.Generic;
using Elsa.App.Inspector.Database;
using Elsa.App.Inspector.Model;

namespace Elsa.App.Inspector.Repo
{
    public interface IInspectionsRepository
    {
        IEnumerable<string> GetInspectionProcedures();

        void RunInspection(InspectionsSyncSession session, string procedureName);

        void RunInspectionAndCloseSession(InspectionsSyncSession session, int issueId);

        void CloseSession(InspectionsSyncSession session);

        InspectionsSyncSession OpenSession();

        List<IssuesSummaryItemModel> GetActiveIssuesSummary();

        List<IInspectionIssue> LoadIssues(int issueTypeId, int pageIndex, int pageSize);

        void OnIssueChanged(int issueId);
        IInspectionIssue LoadIssue(int issueId, bool includeHidden);

        void PostponeIssue(int issueId, int days);
        int ResolveIssueTypeByIssueId(int issueId);
        void LogUserAction(int issueId, string actionText);

        IEnumerable<IInspectionType> GetIssueTypes();
        IEnumerable<IInspectionResponsibilityMatrix> GetResponsibilityMatrix();

        int SetResponsibleUser(int issueTypeId, int userId, string emailOverride, int daysAfterDetect);

        int RemoveResponsibleUser(int issueTypeId, int? userId);

    }
}
