using Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure;
using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class ColumnFactory
    {
        private static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private static readonly Dictionary<string, Type> _columnTypes = new Dictionary<string, Type>();
        private static List<ColumnInfo> _orderedColumns = new List<ColumnInfo>();
        private static bool _initialized = false;

        private readonly IServiceLocator _serviceLocator;

        public ColumnFactory(IServiceLocator locator)
        {
            _serviceLocator = locator;

            if (_initialized || !_lock.TryEnterWriteLock(1))
                return;

            try
            {
                if (_columnTypes.Count > 0)
                    return;

                var baseType = typeof(IDynamicColumnProvider);
                var colNamespace = baseType.Namespace;

                var instances = new List<IDynamicColumnProvider>();

                foreach (var type in baseType.Assembly
                    .GetTypes()
                    .Where(t =>
                        !t.IsAbstract
                        && baseType.IsAssignableFrom(t)))
                {
                    var instace = locator.InstantiateNow(type) as IDynamicColumnProvider;
                    if (instace == null)
                        continue;

                    instances.Add(instace);
                }

                var allCols = new List<ColumnInfo>();
                foreach (var inst in instances)
                {
                    foreach(var columnInfo in inst.GetAvailableColumns())
                    {
                        _columnTypes.Add(columnInfo.Id, inst.GetType());
                        allCols.Add(columnInfo);
                    }
                }

                _orderedColumns.AddRange(allCols.OrderBy(c => c.DisplayOrder));

                _initialized = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public List<DynamicColumnWrapper> GetColumns(string[] ids)
        {
            if (!_initialized)
            {
                _lock.EnterReadLock();
                try
                {
                    return GetColumns(ids);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            var definitions = GetAllDefinedColumns();
            var result = new List<DynamicColumnWrapper>(ids.Length);

            foreach (var columnId in ids)
            {
                if (!_columnTypes.TryGetValue(columnId, out var type))
                    throw new ArgumentException($"Unknown column '{columnId}'");

                var definition = definitions.Single(d => d.Id == columnId);
                var provider = (IDynamicColumnProvider)_serviceLocator.InstantiateNow(type);

                result.Add(new DynamicColumnWrapper(definition, provider));
            }

            return result.OrderBy(r => r.Column.DisplayOrder).ToList();
        }

        public List<ColumnInfo> GetAllDefinedColumns()
        {
            if (!_initialized)
            {
                _lock.EnterReadLock();
                try
                {
                    return GetAllDefinedColumns();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return _orderedColumns;
        }        
    }
}
