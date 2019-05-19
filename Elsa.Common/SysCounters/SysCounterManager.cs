using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;

using Elsa.Core.Entities.Commerce.Common.SystemCounters;

using Robowire.RobOrm.Core;

namespace Elsa.Common.SysCounters
{
    public class SysCounterManager : ISysCountersManager
    {
        private static readonly ConcurrentDictionary<string, object> s_locks =
            new ConcurrentDictionary<string, object>();

        private readonly IDatabase m_database;

        private readonly ISession m_session;

        public SysCounterManager(IDatabase database, ISession session)
        {
            m_database = database;
            m_session = session;
        }

        public void WithCounter(int counterTypeId, Action<string> newValueCallback)
        {
            var lockKey = $"{counterTypeId}_lock_{m_session.Project.Id}";
            var lockObj = s_locks.GetOrAdd(lockKey, k => new object());

            try
            {
                Monitor.Enter(lockObj);

                using (var tx = m_database.OpenTransaction())
                {
                    var counter = m_database.SelectFrom<ISystemCounter>()
                        .Where(c => (c.ProjectId == m_session.Project.Id) && (c.Id == counterTypeId)).Take(1).Execute()
                        .FirstOrDefault();

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

                    m_database.Save(counter);

                    tx.Commit();
                }
            }
            finally
            {
                Monitor.Exit(lockObj);
            }
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
