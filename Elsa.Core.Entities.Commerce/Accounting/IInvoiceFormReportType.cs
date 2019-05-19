using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IInvoiceFormReportType : IProjectRelatedEntity
    {
        int Id { get; }
        
        [NVarchar(100, false)]
        string Name { get; set; }

        [NVarchar(300, false)]
        string ViewControlUrl { get; set; }

        [NVarchar(300, true)]
        string DataSourceUrl { get; set; }

        int ViewOrder { get; set; }
    }
}
