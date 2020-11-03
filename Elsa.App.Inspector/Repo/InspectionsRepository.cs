using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.App.Inspector.Database;
using Elsa.App.Inspector.Model;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RobOrm.Core;

namespace Elsa.App.Inspector.Repo
{
    public class InspectionsRepository : IInspectionsRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly ICache m_cache;
        private readonly ILog m_log;

        public InspectionsRepository(IDatabase database, ISession session, ICache cache, ILog log)
        {
            m_database = database;
            m_session = session;
            m_cache = cache;
            m_log = log;
        }

        public IEnumerable<string> GetInspectionProcedures()
        {
            return m_database.Sql().Call("inspfw_getInspectionProcedures").MapRows(reader => reader.GetString(0));
        }

        public void RunInspection(InspectionsSyncSession session, string procedureName)
        {
            session.Sync(() =>
            {
                try
                {
                    // @spName VARCHAR(300), @projectId INT, @sessionId INT, @retryIssueId INT = null

                    m_database.Sql().Call("inspfw_runInspection")
                        .WithParam("@sessionId", session.SessionId)
                        .WithParam("@projectId", m_session.Project.Id)
                        .WithParam("@spName", procedureName)
                        .NonQuery();
                }
                catch (Exception ex)
                {
                    m_log.Error($"Inspection failed sp={procedureName}, sessionId={session.SessionId}", ex);
                }
            });
        }

        public void RunInspectionAndCloseSession(InspectionsSyncSession session, int issueId)
        {
            session.Sync(() =>
            {
                using (var tx = m_database.OpenTransaction())
                {
                    try
                    {
                        m_database.Sql().Call("inspfw_runInspection")
                            .WithParam("@sessionId", session.SessionId)
                            .WithParam("@projectId", m_session.Project.Id)
                            .WithParam("@retryIssueId", issueId)
                            .NonQuery();
                    }
                    catch (Exception ex)
                    {
                        m_log.Error($"Inspection failed sessionId={session.SessionId}, issueId={issueId}", ex);

                        try
                        {
                            CloseSession(session);
                        }
                        catch (Exception e)
                        {
                            m_log.Error($"Cannot close session sessionId={session.SessionId}, issueId={issueId}", e);
                        }

                        throw;
                    }

                    m_database.Sql().Call("inspfw_closeSession")
                        .WithParam("@projectId", m_session.Project.Id)
                        .WithParam("@sessionId", session.SessionId)
                        .WithParam("@forIssueId", issueId)
                        .NonQuery();

                    tx.Commit();
                }
            });
        }

        public void CloseSession(InspectionsSyncSession session)
        {
            session.Sync(() =>
            {
                try
                {
                    m_database.Sql().Call("inspfw_closeSession")
                        .WithParam("@projectId", m_session.Project.Id)
                        .WithParam("@sessionId", session.SessionId)
                        .NonQuery();
                }
                catch (Exception e)
                {
                    m_log.Error($"Cannot close inspection session", e);
                }
            });
        }

        public void RunInspection(int sessionId, string procedureName)
        {
            try
            {
                m_database.Sql().Call(procedureName).WithParam("@sessionId", sessionId)
                    .WithParam("@projectId", m_session.Project.Id).NonQuery();
            }
            catch (Exception ex)
            {
                m_log.Error($"Inspection failed sp={procedureName}, sessionId={sessionId}", ex);
            }
        }

        public void CloseSession(int sessionId)
        {
            try
            {
                m_database.Sql().Call("inspfw_closeSession").WithParam("@sessionId", sessionId).NonQuery();
            }
            finally
            {
                OnIssueChanged(0);
            }
        }

        public InspectionsSyncSession OpenSession()
        {

            var sync = new InspectionsSyncSession();

            try
            {
                using (var tx = m_database.OpenTransaction())
                {
                    var sid = m_database.Sql().Call("inspfw_openSession")
                        .WithParam("@projectId", m_session.Project.Id)
                        .Scalar<int>();

                    if (sid < 0)
                    {
                        throw new InvalidOperationException(
                            "Inspektor je právě zaneprázdněn, nebo mimo provoz. Zkuste to později");
                    }

                    tx.Commit();
                    sync.SessionId = sid;
                }
            }
            catch
            {
                try
                {
                    sync.Dispose();
                }
                catch
                {
                }

                throw;
            }

            return sync;
        }

        public List<IssuesSummaryItemModel> GetActiveIssuesSummary()
        {
            return m_cache.ReadThrough(GetIssuesOverviewCacheKey(), TimeSpan.FromMinutes(5), () =>
            {
                var result = new List<IssuesSummaryItemModel>();

                m_database.Sql().Call("inspfw_getActiveIssuesSummary")
                    .WithParam("@projectId", m_session.Project.Id)
                    .WithParam("@userId", m_session.User.Id)
                    .ReadRows<int, string, int>(
                        (id, name, count) =>
                        {
                            result.Add(new IssuesSummaryItemModel()
                            {
                                TypeId = id,
                                TypeName = name,
                                IssuesCount = count
                            });
                        });

                return result;
            });
        }

        public List<IInspectionIssue> LoadIssues(int issueTypeId, int pageIndex, int pageSize)
        {
            //TODO optimize

            var now = DateTime.Now;
            var idsQuery = m_database.SelectFrom<IInspectionIssue>()
                .Where(i => i.ProjectId == m_session.Project.Id)
                .Where(i => i.InspectionTypeId == issueTypeId)
                .Where(i => i.ResolveDt == null || i.ResolveDt > now)
                .Where(i => i.PostponedTill == null || i.PostponedTill < now)
                .OrderByDesc(i => i.FirstDetectDt)
                .Take(pageSize);

            if (pageIndex > 0)
            {
                idsQuery = idsQuery.Skip(pageSize * pageIndex);
            }

            var issueIds = idsQuery.Execute().Select(i => i.Id).ToList();

            if (!issueIds.Any())
            {
                return new List<IInspectionIssue>(0);
            }

            var query = m_database.SelectFrom<IInspectionIssue>()
                .Join(i => i.Actions)
                .Join(i => i.Data)
                .Where(i => i.Id.InCsv(issueIds))
                .Where(i => i.ProjectId == m_session.Project.Id)
                .OrderByDesc(i => i.FirstDetectDt);
              
            return query.Execute().ToList();
        }

        public void OnIssueChanged(int issueId)
        {
            m_cache.Remove(GetIssuesOverviewCacheKey());
        }

        public IInspectionIssue LoadIssue(int issueId, bool includeHidden)
        {
            var now = DateTime.Now;
            var query = m_database.SelectFrom<IInspectionIssue>()
                .Join(i => i.Actions)
                .Join(i => i.Data)
                .Where(i => i.Id == issueId)
                .Where(i => i.ProjectId == m_session.Project.Id)
                .OrderByDesc(i => i.FirstDetectDt);

            if (!includeHidden)
            {
                query = query.Where(i => i.ResolveDt == null || i.ResolveDt > now)
                    .Where(i => i.PostponedTill == null || i.PostponedTill < now);
            }

            return query.Execute().FirstOrDefault();
        }

        public void PostponeIssue(int issueId, int days)
        {
            var issue = LoadIssue(issueId, false).Ensure();
            issue.PostponedTill = DateTime.Now.AddDays(days);
            m_database.Save(issue);

            OnIssueChanged(issueId);
        }

        public int ResolveIssueTypeByIssueId(int issueId)
        {
            return m_database.Sql().Execute("SELECT TOP 1 InspectionTypeId FROM InspectionIssue WHERE Id=@id")
                .WithParam("@id", issueId).Scalar<int?>() ?? -1;
        }

        public void LogUserAction(int issueId, string actionText)
        {
            var entry = m_database.New<IInspectionIssueActionsHistory>();
            entry.ActionDt = DateTime.Now;
            entry.ActionText = actionText;
            entry.InspectionIssueId = issueId;
            entry.UserId = m_session.User.Id;

            m_database.Save(entry);
        }

        private string GetIssuesOverviewCacheKey()
        {
            return $"inspIssuesOverview_{m_session.User.Id}";
        }
    }
}
