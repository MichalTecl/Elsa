using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IOrderPriceElement : IOrderRelatedEntity
    {
        long Id { get; }

        [NotFk]
        long ExternalId { get; set; }
                
        [NVarchar(255, true)]
        string TypeName { get; set; }

        [NVarchar(255, true)]
        string Title { get; set; }

        decimal? Price { get; set; }

        decimal? Tax { get; set; }
    }
}
