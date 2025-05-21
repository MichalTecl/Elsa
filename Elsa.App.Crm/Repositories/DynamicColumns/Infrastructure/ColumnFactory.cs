using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure
{
    public class ColumnFactory
    {
        // Use a simple static lock for thread-safe one-time initialization
        private static readonly object _initLock = new object();
        private static readonly Dictionary<string, Type> _columnTypes = new Dictionary<string, Type>();
        private static readonly List<ColumnInfo> _orderedColumns = new List<ColumnInfo>();
        private static bool _initialized = false;

        private readonly IServiceLocator _serviceLocator;

        public ColumnFactory(IServiceLocator locator)
        {
            _serviceLocator = locator ?? throw new ArgumentNullException(nameof(locator));
            EnsureInitialized(locator);
        }

        private static void EnsureInitialized(IServiceLocator locator)
        {
            if (_initialized)
                return;

            lock (_initLock)
            {
                if (_initialized)
                    return;

                var baseType = typeof(IDynamicColumnProvider);

                var instances = new List<IDynamicColumnProvider>();

                foreach (var type in baseType.Assembly
                    .GetTypes()
                    .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t)))
                {
                    var instance = locator.InstantiateNow(type) as IDynamicColumnProvider;
                    if (instance == null)
                        continue;

                    instances.Add(instance);
                }

                var allCols = new List<ColumnInfo>();
                foreach (var inst in instances)
                {
                    foreach (var columnInfo in inst.GetAvailableColumns())
                    {
                        _columnTypes[columnInfo.Id] = inst.GetType();
                        allCols.Add(columnInfo);
                    }
                }

                _orderedColumns.AddRange(allCols.OrderBy(c => c.DisplayOrder));
                _initialized = true;
            }
        }

        public List<DynamicColumnWrapper> GetColumns(string[] ids)
        {
            var result = new List<DynamicColumnWrapper>(ids.Length);

            foreach (var columnId in ids)
            {
                if (!_columnTypes.TryGetValue(columnId, out var type))
                    throw new ArgumentException($"Unknown column '{columnId}'");

                var definition = _orderedColumns.SingleOrDefault(d => d.Id == columnId);
                if (definition == null)
                    throw new ArgumentException($"Column definition not found for '{columnId}'");

                var provider = (IDynamicColumnProvider)_serviceLocator.InstantiateNow(type);

                result.Add(new DynamicColumnWrapper(definition, provider));
            }

            return result.OrderBy(r => r.Column.DisplayOrder).ToList();
        }

        public List<ColumnInfo> GetAllDefinedColumns()
        {
            // Return a copy to prevent external modification
            return _orderedColumns.ToList();
        }
    }
}
