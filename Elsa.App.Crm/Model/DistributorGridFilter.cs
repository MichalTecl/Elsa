using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class DistributorGridFilter
    {
        public string TextFilter { get; set; }

        public List<int> Tags { get; set; }

        public int? SalesRepresentativeId { get; set; }

        public int? CustomerGroupTypeId { get; set; }

        public bool IncludeDisabled { get; set; }

        public List<DistributorFiltersGroup> ExFilterGroups { get; set; }

        [JsonProperty("gridColumns")]
        public List<GridColumnModel> GridColumns { get; set; } = new List<GridColumnModel>();
    }

    public class DistributorFiltersGroup 
    {
        public List<DistributorFilterModel> Filters { get; set; }
    }

    public class GridColumnModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]        
        public string Title { get; set; }

        [JsonProperty("isSelected")]
        public bool IsSelected { get; set; }
    }

}
