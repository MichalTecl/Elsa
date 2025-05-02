using Elsa.App.Crm.Repositories;
using Elsa.Common.DbUtils;
using Elsa.Common.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class DistributorFilterModel
    {
        public DistributorFilterModel() { }

        public DistributorFilterModel(ProcedureInfo pi) : this()
        {
            Title = pi.Tags.GetOrDefault("Title", pi.ProcedureName);
            Description = pi.Tags.GetOrDefault("Note") ?? pi.Tags.GetOrDefault("Description") ?? string.Empty;
            ProcedureName = pi.ProcedureName;

            foreach(var p in pi.Parameters)
            {
                var pmodel = new DistributorFilterParameter();
                pmodel.Name = p;
                pmodel.Control = pi.Tags.GetOrDefault($"{p}.control");
                pmodel.Label = pi.Tags.GetOrDefault($"{p}.label");

                if (!string.IsNullOrWhiteSpace(pmodel.Control))
                    Parameters.Add(pmodel);
            }

            HasFilterTextParameter = pi.Parameters.Any(p => p == DistributorFiltersRepository.FilterTextProcedureParameterName);
        }

        public string ProcedureName { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public bool Inverted { get; set; }

        public List<DistributorFilterParameter> Parameters { get; } = new List<DistributorFilterParameter>();

        public bool HasFilterTextParameter { get; set; }

        internal string GetCacheKey()
        {
            var paramStr = string.Join(",", Parameters.Select(p => $"{p.Name}:{p.Value}"));

            return $"crmfilter:{Title}_{ProcedureName}{(Inverted ? "!" : "")}({paramStr})";
        }
    }

    public class DistributorFilterParameter
    {        
        public string Name { get; set; }
        public string Control { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
    }

    public class FilterExecutionResult
    {
        public HashSet<int> Ids { get; set; }
        public string FilterText { get; set; }
    }
}
