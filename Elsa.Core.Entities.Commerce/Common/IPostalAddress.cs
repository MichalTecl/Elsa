using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Common
{
    public interface IPostalAddress
    {
        [NVarchar(128, true)]
        string Street { get; set; }

        [NVarchar(64, true)]
        string DescriptiveNumber { get; set; }

        [NVarchar(64, true)]
        string OrientationNumber { get; set; }

        [NVarchar(128, true)]
        string City { get; set; }

        [NVarchar(16, true)]
        string Zip { get; set; }
    }
}
