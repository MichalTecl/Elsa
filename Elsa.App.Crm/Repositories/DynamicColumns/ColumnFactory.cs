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

                var baseType = typeof(DynamicColumnBase);
                var colNamespace = baseType.Namespace;

                var instances = new List<DynamicColumnBase>();

                foreach (var type in baseType.Assembly
                    .GetTypes()
                    .Where(t =>
                           t.Namespace == colNamespace
                        && !t.IsAbstract
                        && baseType.IsAssignableFrom(t)))
                {
                    var instace = locator.InstantiateNow(type) as DynamicColumnBase;
                    if (instace == null)
                        continue;

                    instances.Add(instace);
                }

                foreach (var inst in instances.OrderBy(i => i.DisplayOrder))
                {
                    _columnTypes[inst.Id] = inst.GetType();
                    _orderedColumns.Add(new ColumnInfo { Id = inst.Id, Title = inst.Title });
                }

                _initialized = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public List<DynamicColumnBase> GetColumns(string[] names)
        {
            if (!_initialized)
            {
                _lock.EnterReadLock();
                try
                {
                    return GetColumns(names);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            var result = new List<DynamicColumnBase>(names.Length);

            foreach (var name in names)
            {
                if (!_columnTypes.TryGetValue(name, out var type))
                    throw new ArgumentException($"Unknown column '{name}'");

                result.Add((DynamicColumnBase)_serviceLocator.InstantiateNow(type));
            }

            return result.OrderBy(r => r.DisplayOrder).ToList();
        }

        public List<ColumnInfo> GetAllColumnNames()
        {
            if (!_initialized)
            {
                _lock.EnterReadLock();
                try
                {
                    return GetAllColumnNames();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return _orderedColumns;
        }

        public class ColumnInfo
        {
            public string Id { get; set; }
            public string Title { get; set; }
        }
    }
}
