using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface ICrmRobot : ICustomDistributorFilter
    {
        [NVarchar(1000, false)]
        string Description { get; set; }        
        int TagTypeId { get; set; }
        ICustomerTagType CustomerTagType { get; }
        bool FilterMatchSetsTag { get; set; }
        bool FilterUnmatchRemovesTags { get; set; }
        bool FilterMatchRemovesTag {  get; set; }
        bool FilterUnmatchSetsTag { get; set; }
        [NVarchar(1000, true)]
        string NotifyMailList { get; set; }
        DateTime ActiveFrom { get; set; }
        DateTime? ActiveTo { get; set; }
    }
}
