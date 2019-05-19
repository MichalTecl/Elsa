using Elsa.Core.Entities.Commerce.Common.SystemCounters;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IInvoiceFormType : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(300, false)]
        string Name { get; set; }

        [NVarchar(100, false)]
        string GeneratorName { get; set; }

        int? SystemCounterId { get; set; }
        ISystemCounter SystemCounter { get; }
    }
}
