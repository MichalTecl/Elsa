using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common.Widgets
{
    [Entity]
    public interface IAppWidget
    {
        int Id { get; }

        [NVarchar(64, false)]
        string Name { get; set; }

        bool IsAnonymous { get; set; }

        int ViewOrder { get; set; }

        [NVarchar(256, false)]
        string WidgetUrl { get; set; }
    }
}
