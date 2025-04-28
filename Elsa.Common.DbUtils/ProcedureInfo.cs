using System;
using System.Collections.Generic;

namespace Elsa.Common.DbUtils
{
    public class ProcedureInfo
    {
        public string ProcedureName { get; set; }
        public List<string> Parameters { get; } = new List<string>();

        public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>(); 
    }
}
