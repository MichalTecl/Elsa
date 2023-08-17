using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common
{
    [Entity]
    public interface IAddress : IPostalAddress
    {
        int Id { get; }

        [NVarchar(128, true)]
        string CompanyName { get; set; }

        [NVarchar(128, false)]
        string FirstName { get; set; }

        [NVarchar(128, false)]
        string LastName { get; set; }
               
        [NVarchar(128, false)]
        string Country { get; set; }

        [NVarchar(128, false)]
        string Phone { get; set; }

        [NVarchar(-1, true)]
        string Note { get; set; }

        [NVarchar(16, true)]
        string Lat { get; set; }

        [NVarchar(16, true)]
        string Lon { get; set; }

        [NVarchar(512, true)]
        string GeoInfo { get; set; }

        [NVarchar(512, true)]
        string GeoQuery { get; set; }
    }
}
