using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using RobOrm.Core;
using RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IReportingMaterialGroup : IProjectRelatedEntity, IIntIdEntity
    {        
        [NVarchar(255, false)]
        string Name { get; set; }

        int? DisplayOrder { get; set; }
    }

    [Entity]
    public interface IReportingMaterialGroupMaterial : IIntIdEntity 
    {
        int MaterialId { get; set; }
        IMaterial Material { get; }

        int GroupId { get; set; }
        IReportingMaterialGroup Group { get; }        
    }

}
