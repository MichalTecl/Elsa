using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Utils
{
    public sealed class EntityState
    {
        public EntityState()
        {
            Items = new List<EntityStateItem>();
        }

        public List<EntityStateItem> Items { get; private set; }

        public EntityState Add(string key, object value)
        {
            var strVal = value == null ? string.Empty : value.ToString();

            Items.Add(new EntityStateItem(key, strVal));
            return this;
        }

        public void FindChanges(EntityState other, Action<string, string, string> onChange)
        {
            var allKeys = new HashSet<string>(Items.Select(i => i.Key), StringComparer.InvariantCultureIgnoreCase);
            allKeys.UnionWith(other.Items.Select(i => i.Key));
            foreach (var key in allKeys)
            {
                var thisItem = Items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.InvariantCultureIgnoreCase));
                var otherItem = other.Items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.InvariantCultureIgnoreCase));
                var thisVal = thisItem?.Value ?? string.Empty;
                var otherVal = otherItem?.Value ?? string.Empty;
                if (!string.Equals(thisVal, otherVal, StringComparison.InvariantCulture))
                {
                    onChange(key, otherVal, thisVal);
                }
            }
        }
    }

    public class EntityStateItem
    {
        public EntityStateItem(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; private set; }
        public string Value { get; private set; }
    }
}
