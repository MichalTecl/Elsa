using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Jobs.AutomaticQueries.Components;
using Elsa.Jobs.AutomaticQueries.Database;
using Elsa.Jobs.Common;
using Elsa.Smtp.Core;
using OfficeOpenXml;
using Robowire.RobOrm.Core;

namespace Elsa.Jobs.AutomaticQueries
{
    public class RunAutoqueriesJob : IExecutableJob
    {
        private readonly ISession m_session;
        private readonly IDatabase m_database;
        private readonly ILog m_log;
        private readonly IParametersResolver m_paramResolver;
        private readonly IMailSender m_mailSender;

        public RunAutoqueriesJob(ISession session, IDatabase database, ILog log, IParametersResolver paramResolver, IMailSender mailSender)
        {
            m_session = session;
            m_database = database;
            m_log = log;
            m_paramResolver = paramResolver;
            m_mailSender = mailSender;
        }

        public void Run(string customDataJson)
        {
            var queries = m_database.SelectFrom<IAutomaticQuery>().Join(q => q.Parameters)
                .Where(q => q.ProjectId == m_session.Project.Id).Execute().ToList();

            m_log.Info($"Loaded {queries.Count} of AutomaticQueries");

            foreach (var automaticQuery in queries)
            {
                m_log.Info($"Starting processing AutomaticQuery {automaticQuery.TitlePattern}");
                try
                {
                    var parameters =
                        m_paramResolver.ResolveParams(automaticQuery.Parameters, automaticQuery.TitlePattern);
                    
                    if ((automaticQuery.LastTriggerValue ?? string.Empty).Equals(parameters.Trigger))
                    {
                        m_log.Info($"Trigger didn't change, skipping query");
                        continue;
                    }

                    m_log.Info($"{automaticQuery.TitlePattern}: lastTrigger={automaticQuery.LastTriggerValue ?? "NULL"} newTrigger={parameters.Trigger}");

                    string tempFile;
                    using (var table = Execute(automaticQuery.ProcedureName, parameters.Parameters))
                    {
                        tempFile = SaveToTempFile(parameters.TransformedTitle, table);

                        m_mailSender.SendToGroup(automaticQuery.MailRecipientGroup, parameters.TransformedTitle, $"V příloze je nový report \"{parameters.TransformedTitle}\"", tempFile);
                    }

                    automaticQuery.LastTriggerValue = parameters.Trigger;
                    m_database.Save(automaticQuery);

                    try
                    {
                        Directory.Delete(Path.GetDirectoryName(tempFile), true);
                    }
                    catch{;}

                    m_log.Info($"AutoQuery \"{parameters.TransformedTitle}\" successful");
                }
                catch (Exception ex)
                {
                    m_log.Error($"AutoQuery {automaticQuery.TitlePattern} execution failed", ex);
                }
            }
        }

        private DataTable Execute(string procedureName, Dictionary<string, object> parameters)
        {
            var query = m_database.Sql().Call(procedureName);

            foreach (var p in parameters)
            {
                query = query.WithParam(p.Key, p.Value);
            }

            return query.Table();
        }

        private string SaveToTempFile(string title, DataTable table)
        {
            var tempDir = $"C:\\Elsa\\Temp\\AutoQueries\\{m_session.Project.Name}\\{Guid.NewGuid():N}";
            Directory.CreateDirectory(tempDir);

            var file = Path.Combine(tempDir, $"{StringUtil.SanitizeFileName(title)}.xlsx");

            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add(title);
                sheet.Cells["A1"].LoadFromDataTable(table, true);

                package.SaveAs(new FileInfo(file));
            }

            return file;
        }
    }
}
