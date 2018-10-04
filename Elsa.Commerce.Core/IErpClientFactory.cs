using System.Collections.Generic;

namespace Elsa.Commerce.Core
{
    public interface IErpClientFactory
    {
        IErpClient GetErpClient(int erpId);

        IEnumerable<IErpClient> GetAllErpClients();
    }
}
