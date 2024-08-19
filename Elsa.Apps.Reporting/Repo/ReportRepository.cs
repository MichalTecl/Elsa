using Elsa.Apps.Reporting.Model;
using Elsa.Common.Caching;
using Elsa.Common.Utils;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Elsa.Apps.Reporting.Repo
{
    public class ReportRepository
    {
        private readonly IDatabase _database;
        private readonly ICache _cache;

        public ReportRepository(IDatabase database, ICache cache)
        {
            _database = database;
            _cache = cache;
        }

        public List<ReportTypeModel> GetReportTypes()
        {
            var result = new List<ReportTypeModel>();

            _database
                .Sql()
                .Call("ListReportProcedures")
                .ReadRows<string, string>((spName, code) => result.Add(CreateType(spName, code)));

            return result;
        }


        private static ReportTypeModel CreateType(string spName, string code)
        {
            var tags = ExtractSQLComments(code);

            return new ReportTypeModel
            {
                Code = spName,
                Title = tags.GetOrDefault("Title", spName),
                Note = tags.GetOrDefault("Note")
            };
        }

        private static Dictionary<string, string> ExtractSQLComments(string input)
        {
            const string regex = @"--.*?$|/\*.*?\*/"; // -- comment or /* comment */
                        
            MatchCollection matches = Regex.Matches(input, regex, RegexOptions.Singleline | RegexOptions.Multiline);

            var result = new Dictionary<string, string>(matches.Count);

            // Přidání všech nalezených komentářů do seznamu
            foreach (Match match in matches)
            {
                var val = match.Value.Trim();
                if (val.StartsWith("/*"))
                {
                    val = val.Substring(2, val.Length - 4);
                    if (val.EndsWith("*/"))
                    {
                        val = val.Substring(0, val.Length - 2);
                    }
                }
                else if (val.StartsWith("--"))
                {
                    val = val.Substring(2);
                }

                var parts = val.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    result[parts[0].Trim()] = parts[1].Trim();
                }               
            }

            return result;
        }
    }
}
