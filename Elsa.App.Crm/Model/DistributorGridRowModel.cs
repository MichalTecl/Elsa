using Elsa.Common.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class DistributorGridRowModel : DistributorModelBase
    {     
        public DateTime? LastContactDt { get; set; }
        public DateTime? FutureContactDt { get; set; }
        public int TotalOrdersCount { get; set; }
        public decimal TotalOrdersUntaxedPrice { get; set; }       
        public string TotalOrdersPriceF => TotalOrdersUntaxedPrice.ToString("N0", new CultureInfo("cs-CZ"));

        [JsonIgnore]
        public string SearchTag { get; set; }
        public List<SalesTrendTick> TrendModel { get; set; }

        public Dictionary<string, object> DynamicColumns { get; } = new Dictionary<string, object>();

        #region Comparison
        private sealed class Comparer : IComparer<DistributorGridRowModel>
        {
            private readonly Func<DistributorGridRowModel, IComparable> _sortValueGetter;
            private readonly bool _descending;

            public Comparer(Func<DistributorGridRowModel, IComparable> sortValueGetter, bool descending)
            {
                _sortValueGetter = sortValueGetter;
                _descending = descending;
            }

            public int Compare(DistributorGridRowModel x, DistributorGridRowModel y)
            {
                if (_descending)
                    return _sortValueGetter(y).CompareTo(_sortValueGetter(x));
                else
                    return _sortValueGetter(x).CompareTo(_sortValueGetter(y));
            }

            public Comparer GetDescending()
            {
                return new Comparer(_sortValueGetter, true);
            }
        }

        public static IComparer<DistributorGridRowModel> GetComparer(Func<DistributorGridRowModel, IComparable> getter, bool descending)
        {
            return new Comparer(getter, descending);
        }
        #endregion
    }
}
