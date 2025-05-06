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

        int? FilterMatchSetsTagTypeId { get; set; }
        ICustomerTagType FilterMatchSetsTagType { get; }

        int? FilterUnmatchSetsTagTypeId { get; set; }
        ICustomerTagType FilterUnmatchSetsTagType { get; }

        int? FilterMatchRemovesTagTypeId { get; set; }
        ICustomerTagType FilterMatchRemovesTagType { get; }

        int? FilterUnmatchRemovesTagTypeId { get; set; }
        ICustomerTagType FilterUnmatchRemovesTagType { get; }

        [NVarchar(1000, true)]
        string NotifyMailList { get; set; }

        DateTime ActiveFrom { get; set; }
        DateTime? ActiveTo { get; set; }

        int SequenceOrder { get; set; }
    }
}
