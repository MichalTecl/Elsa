using System;
using System.Collections.Generic;
using System.Data;
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

        private string ResponsibilityMatrixCacheKey => $"inspMatrix_{m_session.Project.Id}";
        private string IssueTypesCacheKey => $"inspIssueTypes_{m_session.Project.Id}";

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

                    using (var table = m_database.Sql().Call("inspfw_runInspection")
                        .WithParam("@sessionId", session.SessionId)
                        .WithParam("@projectId", m_session.Project.Id)
                        .WithParam("@spName", procedureName)
                        .Table())
                    {
                        if (table.Rows.Count == 0)
                        {
                            m_log.Info($"Inspection {procedureName} returned no data");
                            return;
                        }

                        ProcessResults(session, procedureName, table);
                    }
                }
                catch (Exception ex)
                {
                    m_log.Error($"Inspection failed sp={procedureName}, sessionId={session.SessionId}", ex);
                }
            });
        }

        private void ProcessResults(InspectionsSyncSession session, string procedureName, DataTable table)
        {
            const string issueTypeColumn = "IssueType";
            const string issueCodeColumn = "IssueCode";
            const string messageColumn = "Message";
            const string issueDataPrefix = "data:";
            const string actionControlPrefix = "ActionControlUrl";
            const string actionNamePrefix = "ActionName";

            foreach(var row in table.Rows.Cast<DataRow>())
            {
                var issueTypeName = row[issueTypeColumn].ToString();
                var issueCode = row[issueCodeColumn].ToString();
                var message = row[messageColumn].ToString();

                var issueId = AddIssue(session.SessionId, issueTypeName, issueCode, message);

                m_log.Info($"Issue added: type={issueTypeName} code={issueCode} message={message} => id={issueId}");

                // process action data
                foreach(var col in table.Columns.Cast<DataColumn>().Where(c => c.ColumnName.StartsWith(issueDataPrefix)))
                {
                    m_log.Info($"Processing action data {col.ColumnName}");

                    var key = col.ColumnName.Substring(issueDataPrefix.Length);
                    var value = row[col];

                    if (value == DBNull.Value)
                    {
                        m_log.Info($"Action data {key} is null");
                        continue;
                    }

                    if (value is int v)
                        SetIssueData(issueId, key, v);
                    else
                        SetIssueData(issueId, key, value.ToString());
                }

                foreach(var actioncontrolCol in table.Columns.OfType<DataColumn>().Where(c => c.ColumnName.StartsWith(actionControlPrefix)))
                {
                    m_log.Info($"Processing action control {actioncontrolCol.ColumnName}");
                    var actionNameCol = actioncontrolCol.ColumnName.Replace(actionControlPrefix, actionNamePrefix);

                    // throw if action name column is missing
                    if (!table.Columns.Contains(actionNameCol))
                    {
                        throw new InvalidOperationException($"Action name column {actionNameCol} is missing");
                    }

                    if (row.IsNull(actioncontrolCol) || row.IsNull(actionNameCol))
                    {
                        m_log.Info("Action control url or name is null");
                        continue;
                    }

                    var actionName = row[actionNameCol].ToString();
                    var controlUrl = row[actioncontrolCol].ToString();

                    m_log.Info($"Setting action {actionName} with control {controlUrl}");

                    SetIssueAction(issueId, controlUrl, actionName);
                }
            }
                        
            m_log.Info($"Processing of resultset returned by {procedureName} completed");
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

        
        public IEnumerable<IInspectionType> GetIssueTypes()
        {
            // load inspeciton types through 1 minutes cache. Cache key must be constructed to be unique for each project
            return m_cache.ReadThrough(IssueTypesCacheKey, TimeSpan.FromMinutes(1), () =>
            {
                return m_database.SelectFrom<IInspectionType>().Execute().ToList();
            });
        }

        public IEnumerable<IInspectionResponsibilityMatrix> GetResponsibilityMatrix()
        {
            //load responsibility matrix through 1 minutes cache. Cache key must be constructed to be unique for each project
            return m_cache.ReadThrough(ResponsibilityMatrixCacheKey, TimeSpan.FromMinutes(1), () =>
            {
                return m_database.SelectFrom<IInspectionResponsibilityMatrix>().Execute().ToList();
            });
        }

        public int SetResponsibleUser(int issueTypeId, int userId, string emailOverride, int daysAfterDetect)
        {
            // check whether the user is already responsible for the issue type using GetResponsibilityMatrix method
            var record = GetResponsibilityMatrix().FirstOrDefault(r => r.InspectionTypeId == issueTypeId && r.ResponsibleUserId == userId);
            if (record != null)
            {
                // update existing responsibility matrix record if new values are different
                if (record.EMailOverride == emailOverride && record.DaysAfterDetect == daysAfterDetect)
                    return 0;                
            }
            else
            {
                record = m_database.New<IInspectionResponsibilityMatrix>();
            }
            
            record.InspectionTypeId = issueTypeId;
            record.ResponsibleUserId = userId;
            record.EMailOverride = emailOverride;
            record.DaysAfterDetect = daysAfterDetect;
            
            m_database.Save(record);

            m_cache.Remove(ResponsibilityMatrixCacheKey);

            return 1;
        }

        public int RemoveResponsibleUser(int issueTypeId, int? userId)
        {            
            //check whether the user is responsible for the issue type using GetResponsibilityMatrix method
            var existing = GetResponsibilityMatrix().Where(r => r.InspectionTypeId == issueTypeId && (userId == null || r.ResponsibleUserId == userId)).ToList();
            if (existing.Count == 0)
                return 0;

            // delete the responsibility matrix record
            m_database.DeleteAll(existing);

            // invalidate cache
            m_cache.Remove(ResponsibilityMatrixCacheKey);

            return existing.Count;
        }

        // method calling this procedure: EXEC @issueId = inspfw_addIssue @sessionId, @inspType, @code, @message;
        public int AddIssue(int sessionId, string inspectionTypeName, string code, string message)
        {
            return m_database.Sql().Call("inspfw_addIssueAndSelectId")
                .WithParam("@sessionId", sessionId)
                .WithParam("@typeName", inspectionTypeName)
                .WithParam("@code", code)
                .WithParam("@message", message)
                .Scalar<int>();
        }

        // method calling this procedure: inspfw_setIssueAction @issueId, '/UI/Controls/Inventory/WarehouseControls/WhActions/StockEventThrashActionButton.html', @stockEventTypeName;
        public void SetIssueAction(int issueId, string controlUrl, string actionName)
        {
            m_database.Sql().Call("inspfw_setIssueAction")
                .WithParam("@issueId", issueId)
                .WithParam("@controlUrl", controlUrl)
                .WithParam("@actionName", actionName)
                .NonQuery();
        }

        // method calling this procedure: inspfw_setIssueDataInt @issueId, 'MaterialId', @materialId;
        public void SetIssueData(int issueId, string key, int value)
        {
            m_database.Sql().Call("inspfw_setIssueDataInt")
                .WithParam("@issueId", issueId)
                .WithParam("@property", key)
                .WithParam("@value", value)
                .NonQuery();
        }

        public void SetIssueData(int issueId, string key, string value)
        {
            m_database.Sql().Call("inspfw_setIssueDataString")
                .WithParam("@issueId", issueId)
                .WithParam("@property", key)
                .WithParam("@value", value)
                .NonQuery();
        }

    }
}

