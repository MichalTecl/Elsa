using System;
using System.Collections.Generic;
using Elsa.Commerce.Core.Configuration;
using Elsa.Common.Caching;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using RobOrm.Core;

namespace Elsa.Commerce.Core
{
    public class OrderWeightCalculator : IOrderWeightCalculator
    {
        private readonly ILog _log;
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly OrdersSystemConfig _ordersConfig;

        public OrderWeightCalculator(ILog log, IDatabase database, ICache cache, OrdersSystemConfig ordersConfig)
        {
            _log = log;
            _database = database;
            _cache = cache;
            _ordersConfig = ordersConfig;
        }
        
        public decimal? GetWeight(IPurchaseOrder po)
        {
            if (!_ordersConfig.UseOrderWeight)
                return null;

            var isValid = true;
            decimal sum = 0;

            foreach (var i in GetWeights(po))
            {
                decimal weight = 0;
                if (i.Item3 == null)
                {
                    var inx = GetWeightIndex();

                    if ((!inx.TryGetValue(i.Item1, out var w)) || (w == null))
                    {
                        _log.Error($"No weight found for \"{i.Item1}\"");
                        isValid = false;

                        continue;
                    }

                    weight = w ?? 0;
                }
                else
                {
                    weight = i.Item3.Value;
                }

                sum += weight; // * i.Item2;
            }
            
            return isValid ? (sum + (_ordersConfig.OrderWeightAddition ?? 0)) : (decimal?)null;
        }

        /// <returns>Product|Qty|SavedWeight</returns>
        private IEnumerable<Tuple<string, decimal, decimal?>> GetWeights(IPurchaseOrder po)
        {
            foreach (var item in po.Items)
            {
                var hasChildItems = false;

                foreach (var child in item.KitChildren)
                {
                    hasChildItems = true;

                    yield return new Tuple<string, decimal, decimal?>(child.PlacedName, child.Quantity * item.Quantity, child.Weight);
                }

                if (hasChildItems)
                    continue;

                yield return new Tuple<string, decimal, decimal?>(item.PlacedName, item.Quantity, item.Weight);
            }
        }

        private Dictionary<string, decimal?> GetWeightIndex()
        {
            return _cache.ReadThrough("productWeightIndex", TimeSpan.FromSeconds(10), () =>
            {
                var result = new Dictionary<string, decimal?>();

                _database.Sql().Execute("SELECT PlacedName, Weight FROM vw_productWeightIndex").ReadRows(r =>
                {
                    var w = r.IsDBNull(1) ? null : (decimal?) r.GetDecimal(1);
                    result[r.GetString(0)] = w;
                });

                return result;
            });
        }
    }
}
