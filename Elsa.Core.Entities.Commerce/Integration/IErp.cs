using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Integration
{
    [Entity]
    public interface IErp : IProjectRelatedEntity
    {
        int Id { get; }
        
        [NVarchar(256, false)]
        string Description { get; set; }

        [NVarchar(512, false)]
        string ClientClass { get; set; }

        [NVarchar(-1, true)]
        string ClientData { get; set; }

        [NVarchar(255, true)]
        string IconUrl { get; set; }
    }
}
