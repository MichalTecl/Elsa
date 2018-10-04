using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Commerce.Core
{
    public interface IErpRepository
    {
        IErp GetErp(int id);

        IEnumerable<IErp> GetAllErps();
    }
}
