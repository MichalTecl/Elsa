using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Common.SystemCounters;

using RobOrm.Core;

namespace Elsa.Common.SysCounters
{
    public class SysCounterManager : ISysCountersManager
    {
        private static readonly ConcurrentDictionary<string, object> s_locks =
            new ConcurrentDictionary<string, object>();

        private readonly IDatabase _database;

        private readonly ISession _session;
        private readonly ICache _cache;

        public SysCounterManager(IDatabase database, ISession session, ICache cache)
        {
            _database = database;
            _session = session;
            _cache = cache;
        }

        public void WithCounter(int counterTypeId, Action<string> newValueCallback)
        {
            var lockKey = $"{counterTypeId}_lock_{_session.Project.Id}";
            var lockObj = s_locks.GetOrAdd(lockKey, k => new object());

            try
            {
                Monitor.Enter(lockObj);

                using (var tx = _database.OpenTransaction())
                {
                    var counter = GetCounter(counterTypeId);

                    if (counter == null)
                    {
                        throw new InvalidOperationException($"Counter Id={counterTypeId} does not exist");
                    }

                    var now = DateTime.Now;

                    var nowFormat = FormatDtPart(counter.DtFormat, now);

                    if (!nowFormat.Equals(counter.LastDtValue ?? string.Empty,
                        StringComparison.InvariantCultureIgnoreCase))
                    {
                        counter.CounterValue = counter.CounterMinValue;
                        counter.LastDtValue = nowFormat;
                    }
                    else
                    {
                        counter.CounterValue++;
                    }

                    var sb = new StringBuilder();

                    sb.Append(counter.StaticPrefix?.Trim())
                        .Append(nowFormat?.Trim())
                        .Append(counter.CounterValue.ToString().PadLeft(counter.CounterPadding, '0'))
                        .Append(counter.StaticSuffix?.Trim());

                    newValueCallback(sb.ToString());

                    _database.Save(counter);

                    tx.Commit();
                }
            }
            finally
            {
                Monitor.Exit(lockObj);
                _cache.Remove($"syscounter_{ counterTypeId}");
            }
        }

        public ISystemCounter GetCounter(int counterTypeId)
        {
            return _cache.ReadThrough($"syscounter_{counterTypeId}", TimeSpan.FromMinutes(1), () => _database
                .SelectFrom<ISystemCounter>()
                .Where(c => (c.ProjectId == _session.Project.Id) && (c.Id == counterTypeId)).Take(1).Execute()
                .FirstOrDefault());
        }

        private static string FormatDtPart(string format, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return string.Empty;
            }

            return now.ToString(format);
        }
    }
}
