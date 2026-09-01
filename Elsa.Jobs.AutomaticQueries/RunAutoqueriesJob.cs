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
        private readonly ISession _session;
        private readonly IDatabase _database;
        private readonly ILog _log;
        private readonly IParametersResolver _paramResolver;
        private readonly IMailSender _mailSender;
        private readonly AutoProceduresJob _proceduresJob;

        public RunAutoqueriesJob(ISession session, IDatabase database, ILog log, IParametersResolver paramResolver, IMailSender mailSender, AutoProceduresJob proceduresJob)
        {
            _session = session;
            _database = database;
            _log = log;
            _paramResolver = paramResolver;
            _mailSender = mailSender;
            _proceduresJob = proceduresJob;
        }

        public void Run(string customDataJson)
        {
            var queries = _database.SelectFrom<IAutomaticQuery>().Join(q => q.Parameters)
                .Where(q => q.ProjectId == _session.Project.Id).Execute().ToList();

            _log.Info($"Loaded {queries.Count} of AutomaticQueries");

            foreach (var automaticQuery in queries)
            {
                _log.Info($"Starting processing AutomaticQuery {automaticQuery.TitlePattern}");
                try
                {
                    var parameters =
                        _paramResolver.ResolveParams(automaticQuery.Parameters, automaticQuery.TitlePattern);
                    
                    if ((automaticQuery.LastTriggerValue ?? string.Empty).Equals(parameters.Trigger))
                    {
                        _log.Info($"Trigger didn't change, skipping query");
                        continue;
                    }

                    _log.Info($"{automaticQuery.TitlePattern}: lastTrigger={automaticQuery.LastTriggerValue ?? "NULL"} newTrigger={parameters.Trigger}");

                    string tempFile;
                    using (var table = Execute(automaticQuery.ProcedureName, parameters.Parameters))
                    {
                        tempFile = SaveToTempFile(parameters.TransformedTitle, table);

                        _mailSender.SendToGroup(SenderMailboxType.SystemRobot, automaticQuery.MailRecipientGroup, parameters.TransformedTitle, $"V příloze je nový report \"{parameters.TransformedTitle}\"", tempFile);
                    }

                    automaticQuery.LastTriggerValue = parameters.Trigger;
                    _database.Save(automaticQuery);

                    try
                    {
                        Directory.Delete(Path.GetDirectoryName(tempFile), true);
                    }
                    catch{;}

                    _log.Info($"AutoQuery \"{parameters.TransformedTitle}\" successful");
                }
                catch (Exception ex)
                {
                    _log.Error($"AutoQuery {automaticQuery.TitlePattern} execution failed", ex);
                }
            }

            _proceduresJob.Run();
        }

        private DataTable Execute(string procedureName, Dictionary<string, object> parameters)
        {
            var query = _database.Sql().Call(procedureName);

            foreach (var p in parameters)
            {
                query = query.WithParam(p.Key, p.Value);
            }

            return query.Table();
        }

        private string SaveToTempFile(string title, DataTable table)
        {
            var tempDir = $"C:\\Elsa\\Temp\\AutoQueries\\{_session.Project.Name}\\{Guid.NewGuid():N}";
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
