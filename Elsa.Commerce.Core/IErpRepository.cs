using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Commerce.Core
{
    public interface IErpRepository
    {
        IErp GetErp(int id);
    }
}
