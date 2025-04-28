using Elsa.Common.Caching;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Elsa.Common.DbUtils
{
    public class ProcedureLister : IProcedureLister
    {
        private readonly IDatabase _db;
        private readonly ICache _cache;

        public ProcedureLister(IDatabase db, ICache cache)
        {
            _db = db;
            _cache = cache;
        }

        public List<ProcedureInfo> ListProcedures(string likeNamePattern)
        {
            return _cache.ReadThrough(
                $"proclister_{likeNamePattern}", 
                TimeSpan.FromHours(1), 
                () => ListProceduresFactory(likeNamePattern));
        }

        private List<ProcedureInfo> ListProceduresFactory(string likeNamePattern)
        {
            var result = new List<ProcedureInfo>();

            _db
                .Sql()
                .Call("ListProcedures")
                .WithParam("@like", likeNamePattern)
                .ReadRows<string, string, string>((spName, spCode, paramNames) => result.Add(ParseProcedure(spName, spCode, paramNames)));
            
            return result;
        }

        private ProcedureInfo ParseProcedure(string spName, string spCode, string paramNamesCsv)
        {
            var result = new ProcedureInfo() { ProcedureName = spName };
            
            if(!string.IsNullOrEmpty(spCode)) 
                result.Parameters.AddRange(paramNamesCsv.Split(';'));

            ExtractSQLComments(spCode, result.Tags);

            return result;
        }

        private static void ExtractSQLComments(string input, Dictionary<string, string> target)
        {
            const string regex = @"--.*?$|/\*.*?\*/"; // -- comment or /* comment */

            MatchCollection matches = Regex.Matches(input, regex, RegexOptions.Singleline | RegexOptions.Multiline);

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
                    target[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }
    }
}

