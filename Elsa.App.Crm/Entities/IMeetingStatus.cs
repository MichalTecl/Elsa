using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface IMeetingStatus : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(100, false)]
        string Title { get; set; }

        [NVarchar(32, false)]
        string ColorHex { get; set; }

        [NVarchar(32, true)]
        string IconClass { get; set; }
        
        bool ActionExpected { get; set; }

        bool? MeansCancelled { get; set; }
    }
}
