using Elsa.Core.Entities.Commerce.Integration;
using System.Collections.Generic;

namespace Elsa.Commerce.Core
{
    public interface IOrderImportFailureRepository
    {
        IReadOnlyCollection<IOrderImportFailure> GetPendingForErp(int erpId);

        IReadOnlyCollection<IOrderImportFailure> GetPendingForCurrentProject();

        IReadOnlyCollection<IOrderImportFailure> GetNotNotifiedForCurrentProject();

        int CountPendingForCurrentProject();

        IOrderImportFailure GetPendingById(int failureId);

        void RegisterFailure(int erpId, string orderNumber, string error);

        void Resolve(int erpId, string orderNumber);

        void MarkNotificationsSent(IEnumerable<int> failureIds);
    }
}
