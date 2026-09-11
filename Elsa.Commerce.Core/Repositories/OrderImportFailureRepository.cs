using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Integration;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Aggregations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Commerce.Core.Repositories
{
    public class OrderImportFailureRepository : IOrderImportFailureRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;

        public OrderImportFailureRepository(IDatabase database, ISession session)
        {
            _database = database;
            _session = session;
        }

        public IReadOnlyCollection<IOrderImportFailure> GetPendingForErp(int erpId)
        {
            return _database.SelectFrom<IOrderImportFailure>()
                .Join(f => f.Erp)
                .Where(f => f.ErpId == erpId)
                .Where(f => f.Erp.ProjectId == _session.Project.Id)
                .Where(f => f.ResolveDate == null)
                .OrderBy(f => f.FirstFailureDt)
                .Execute()
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyCollection<IOrderImportFailure> GetPendingForCurrentProject()
        {
            return _database.SelectFrom<IOrderImportFailure>()
                .Join(f => f.Erp)
                .Where(f => f.Erp.ProjectId == _session.Project.Id)
                .Where(f => f.ResolveDate == null)
                .OrderBy(f => f.FirstFailureDt)
                .Execute()
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyCollection<IOrderImportFailure> GetNotNotifiedForCurrentProject()
        {
            return _database.SelectFrom<IOrderImportFailure>()
                .Join(f => f.Erp)
                .Where(f => f.Erp.ProjectId == _session.Project.Id)
                .Where(f => f.EmailNotificationDt == null)
                .OrderBy(f => f.FirstFailureDt)
                .Execute()
                .ToList()
                .AsReadOnly();
        }

        public int CountPendingForCurrentProject()
        {
            return _database.AggregateFrom<IOrderImportFailure>()
                .Join(f => f.Erp)
                .Where(f => f.Erp.ProjectId == _session.Project.Id)
                .Where(f => f.ResolveDate == null)
                .GroupAll<PendingCount>()
                .Bind(f => f.Id.Count(), (result, value) => result.Count = value)
                .Execute()
                .Single()
                .Count;
        }

        public IOrderImportFailure GetPendingById(int failureId)
        {
            return _database.SelectFrom<IOrderImportFailure>()
                .Join(f => f.Erp)
                .Where(f => f.Id == failureId)
                .Where(f => f.Erp.ProjectId == _session.Project.Id)
                .Where(f => f.ResolveDate == null)
                .Execute()
                .FirstOrDefault();
        }

        public void RegisterFailure(int erpId, string orderNumber, string error)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new ArgumentException("Cannot enqueue a failed order without its order number", nameof(orderNumber));

            var failure = _database.SelectFrom<IOrderImportFailure>()
                .Where(f => f.ErpId == erpId)
                .Where(f => f.OrderNumber == orderNumber)
                .Where(f => f.ResolveDate == null)
                .Execute()
                .FirstOrDefault();

            var now = DateTime.Now;
            if (failure == null)
            {
                failure = _database.New<IOrderImportFailure>();
                failure.ErpId = erpId;
                failure.OrderNumber = orderNumber;
                failure.FirstFailureDt = now;
            }

            failure.LastFailureDt = now;
            failure.FailureCount++;
            failure.LastError = error ?? "Unknown order import error";

            _database.Save(failure);
        }

        public void Resolve(int erpId, string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                return;

            var failures = _database.SelectFrom<IOrderImportFailure>()
                .Where(f => f.ErpId == erpId)
                .Where(f => f.OrderNumber == orderNumber)
                .Where(f => f.ResolveDate == null)
                .Execute()
                .ToList();

            var resolveDate = DateTime.Now;
            foreach (var failure in failures)
            {
                failure.ResolveDate = resolveDate;
                _database.Save(failure);
            }
        }

        public void MarkNotificationsSent(IEnumerable<int> failureIds)
        {
            var ids = failureIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0)
                return;

            using (var transaction = _database.OpenTransaction())
            {
                var failures = _database.SelectFrom<IOrderImportFailure>()
                    .Join(f => f.Erp)
                    .Where(f => f.Id.InCsv(ids))
                    .Where(f => f.Erp.ProjectId == _session.Project.Id)
                    .Where(f => f.EmailNotificationDt == null)
                    .Execute()
                    .ToList();

                var notificationDate = DateTime.Now;
                foreach (var failure in failures)
                {
                    failure.EmailNotificationDt = notificationDate;
                    _database.Save(failure);
                }

                transaction.Commit();
            }
        }

        private sealed class PendingCount
        {
            public int Count { get; set; }
        }
    }
}
